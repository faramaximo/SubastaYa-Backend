using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Enums;
using SubastaYa.Domain.Exceptions;

namespace SubastaYa.Application.UseCases.Auctions.Commands;

public record FinalizeAuctionCommand(int SubastaId);

public class FinalizeAuctionCommandHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ILedgerRepository _ledgerRepository;
    private readonly IAuctionNotifier _notifier;
    private readonly IAuditService _auditService;

    public FinalizeAuctionCommandHandler(
        IUnitOfWork unitOfWork,
        IAuctionRepository auctionRepository,
        IWalletRepository walletRepository,
        ILedgerRepository ledgerRepository,
        IAuctionNotifier notifier,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _auctionRepository = auctionRepository;
        _walletRepository = walletRepository;
        _ledgerRepository = ledgerRepository;
        _notifier = notifier;
        _auditService = auditService;
    }

    public async Task<bool> Handle(FinalizeAuctionCommand command, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var subasta = await _auctionRepository.GetByIdWithBidsAsync(command.SubastaId);
            if (subasta is null)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return false;
            }

            // La verificación se repite dentro de la transacción para no liquidar una
            // subasta que fue extendida por anti-sniping después de la consulta del worker.
            if ((subasta.Estado != EstadoSubasta.Activa && subasta.Estado != EstadoSubasta.Programada) ||
                subasta.FechaFin > DateTime.UtcNow)
            {
                await _unitOfWork.RollbackAsync(cancellationToken);
                return false;
            }

            var pujaGanadora = subasta.Pujas
                .OrderByDescending(p => p.Monto)
                .FirstOrDefault();

            if (pujaGanadora != null)
            {
                var billeteraComprador = await _walletRepository.GetByUserIdAsync(pujaGanadora.CompradorId);
                var billeteraVendedor = await _walletRepository.GetByUserIdAsync(subasta.VendedorId);

                // Una puja ganadora debe tener sus fondos retenidos. No se deja la
                // subasta activa silenciosamente: la excepción se registra en el worker.
                if (billeteraComprador is null || billeteraVendedor is null || billeteraComprador.SaldoRetenido < pujaGanadora.Monto)
                {
                    throw new DomainException("No se puede liquidar la subasta: faltan billeteras o fondos retenidos del ganador.");
                }

                // Descontar saldo retenido y total al comprador
                billeteraComprador.ProcesarPagoSubasta(pujaGanadora.Monto);

                // Acreditar saldo al vendedor
                billeteraVendedor.Depositar(pujaGanadora.Monto);

                // Registrar transacciones inmutables en Ledger
                var transaccionPago = new TransaccionLedger
                {
                    BilleteraId = billeteraComprador.Id,
                    Tipo = TipoTransaccion.Pago,
                    Monto = pujaGanadora.Monto,
                    Fecha = DateTime.UtcNow,
                    SubastaId = subasta.Id
                };
                await _ledgerRepository.AgregarAsync(transaccionPago);

                var transaccionCobro = new TransaccionLedger
                {
                    BilleteraId = billeteraVendedor.Id,
                    Tipo = TipoTransaccion.Cobro,
                    Monto = pujaGanadora.Monto,
                    Fecha = DateTime.UtcNow,
                    SubastaId = subasta.Id
                };
                await _ledgerRepository.AgregarAsync(transaccionCobro);

                // Cambiar estado a FINALIZADA
                subasta.FinalizarConGanador();

                // Registrar evento en AuditoriaLog
                await _auditService.RegistrarEventoAsync(
                    entidad: "Subasta",
                    entidadId: subasta.Id,
                    accion: "SUBASTA_FINALIZADA",
                    usuarioId: pujaGanadora.CompradorId,
                    detalle: new
                    {
                        subastaId = subasta.Id,
                        ganadorId = pujaGanadora.CompradorId,
                        montoGanador = pujaGanadora.Monto,
                        vendedorId = subasta.VendedorId,
                        fechaFin = subasta.FechaFin,
                        estado = EstadoSubasta.Finalizada.ToString()
                    },
                    cancellationToken: cancellationToken
                );

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                // Notificar en tiempo real
                await _notifier.NotificarSubastaFinalizadaAsync(subasta.Id, pujaGanadora.CompradorId, pujaGanadora.Monto);
                return true;
            }
            else
            {
                // Subasta sin pujas -> DESIERTA
                subasta.DeclararDesierta();

                await _auditService.RegistrarEventoAsync(
                    entidad: "Subasta",
                    entidadId: subasta.Id,
                    accion: "SUBASTA_DESIERTA",
                    usuarioId: null,
                    detalle: new
                    {
                        subastaId = subasta.Id,
                        vendedorId = subasta.VendedorId,
                        fechaFin = subasta.FechaFin,
                        estado = EstadoSubasta.Desierta.ToString(),
                        motivo = "No se recibieron ofertas válidas durante el período activo."
                    },
                    cancellationToken: cancellationToken
                );

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                await _notifier.NotificarSubastaFinalizadaAsync(subasta.Id, null, 0);
                return true;
            }
        }
        catch (Exception)
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
