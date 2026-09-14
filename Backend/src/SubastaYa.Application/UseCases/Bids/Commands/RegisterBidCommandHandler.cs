using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Enums;
using SubastaYa.Domain.Exceptions;

namespace SubastaYa.Application.UseCases.Bids.Commands;

public class RegisterBidCommandHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ILedgerRepository _ledgerRepository;
    private readonly IAuctionNotifier _notifier;
    private readonly IAuditService _auditService;

    public RegisterBidCommandHandler(
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

    public async Task<PujaResponseDto> Handle(RegisterBidCommand command)
    {
        await _unitOfWork.BeginTransactionAsync();
        PujaResponseDto response;

        try
        {
            var subasta = await _auctionRepository.GetByIdWithBidsAsync(command.SubastaId)
                ?? throw new ResourceNotFoundException("La subasta especificada no existe.");

            if (subasta.Estado != EstadoSubasta.Activa || subasta.FechaFin <= DateTime.UtcNow)
                throw new UnprocessableEntityException("La subasta no se encuentra activa o ya ha finalizado.");

            if (subasta.VendedorId == command.UsuarioId)
                throw new ForbiddenException("El vendedor no puede pujar en su propia subasta.");

            var pujaLiderAnterior = subasta.Pujas
                .OrderByDescending(p => p.Monto)
                .FirstOrDefault();

            var montoMinimoRequerido = pujaLiderAnterior is null
                ? subasta.PrecioBase
                : pujaLiderAnterior.Monto + subasta.IncrementoMinimo;

            if (command.Monto < montoMinimoRequerido)
            {
                throw new UnprocessableEntityException(
                    $"El monto ofertado (${command.Monto}) debe ser al menos de ${montoMinimoRequerido}.");
            }

            var billeteraNuevoOfertante = await _walletRepository.GetByUserIdAsync(command.UsuarioId)
                ?? throw new ResourceNotFoundException("La billetera del usuario no existe.");

            var esMejoraDelMismoLider = pujaLiderAnterior?.CompradorId == command.UsuarioId;
            var montoARetener = esMejoraDelMismoLider
                ? command.Monto - pujaLiderAnterior!.Monto
                : command.Monto;

            if (billeteraNuevoOfertante.SaldoDisponible < montoARetener)
            {
                throw new UnprocessableEntityException(
                    $"Saldo insuficiente. Disponible: ${billeteraNuevoOfertante.SaldoDisponible}, " +
                    $"requerido: ${montoARetener}.");
            }

            // Si otro usuario era líder, se libera su garantía y se registra el movimiento.
            if (pujaLiderAnterior is not null && !esMejoraDelMismoLider)
            {
                var billeteraLiderAnterior =
                    await _walletRepository.GetByUserIdAsync(pujaLiderAnterior.CompradorId)
                    ?? throw new ResourceNotFoundException("La billetera del líder anterior no existe.");

                billeteraLiderAnterior.LiberarFondos(pujaLiderAnterior.Monto);

                await _ledgerRepository.AddAsync(new TransaccionLedger
                {
                    BilleteraId = billeteraLiderAnterior.Id,
                    Tipo = TipoTransaccion.Liberacion,
                    Monto = pujaLiderAnterior.Monto,
                    Fecha = DateTime.UtcNow,
                    SubastaId = subasta.Id
                });
            }

            // Si el mismo líder mejora su oferta, sólo se inmoviliza la diferencia.
            billeteraNuevoOfertante.RetenerFondos(montoARetener);

            await _ledgerRepository.AddAsync(new TransaccionLedger
            {
                BilleteraId = billeteraNuevoOfertante.Id,
                Tipo = TipoTransaccion.Retencion,
                Monto = montoARetener,
                Fecha = DateTime.UtcNow,
                SubastaId = subasta.Id
            });

            var fechaFinAntes = subasta.FechaFin;
            subasta.RegistrarPuja(command.UsuarioId, command.Monto);

            var nuevaPuja = subasta.Pujas.MaxBy(p => p.FechaPuja)
                ?? throw new InvalidOperationException("No se pudo registrar la puja.");

            var antiSnipingActivado = subasta.FechaFin > fechaFinAntes;

            if (antiSnipingActivado)
            {
                await _auditService.RegistrarEventoAsync(
                    entidad: "Subasta",
                    entidadId: subasta.Id,
                    accion: "ANTI_SNIPING_ACTIVADO",
                    usuarioId: command.UsuarioId,
                    detalle: new
                    {
                        subastaId = subasta.Id,
                        usuarioId = command.UsuarioId,
                        montoPuja = command.Monto,
                        fechaFinAnterior = fechaFinAntes,
                        nuevaFechaFin = subasta.FechaFin,
                        segundosExtension = (subasta.FechaFin - fechaFinAntes).TotalSeconds,
                        motivo = "Oferta recibida en los últimos 60 segundos de la subasta."
                    });
            }

            // Puja, billeteras, Ledger y auditoría se persisten juntos.
            await _unitOfWork.SaveChangesAsync();

            response = new PujaResponseDto(
                nuevaPuja.Id,
                subasta.Id,
                command.UsuarioId,
                command.Monto,
                nuevaPuja.FechaPuja,
                subasta.FechaFin,
                antiSnipingActivado);

            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }

        // Sólo se notifica después de confirmar la transacción.
        await _notifier.NotificarNuevaPujaAsync(response);

        return response;
    }
}