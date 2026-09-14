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
        var subasta = await _auctionRepository.GetByIdWithBidsAsync(command.SubastaId);
        if (subasta == null)
        {
            await LanzarRechazoAsync(command.SubastaId, command.UsuarioId, "La subasta especificada no existe.", command.Monto);
        }

        if (subasta!.Estado != EstadoSubasta.Activa || subasta.FechaFin <= DateTime.UtcNow)
        {
            await LanzarRechazoAsync(command.SubastaId, command.UsuarioId, "La subasta no se encuentra activa o ya ha finalizado.", command.Monto, new { estadoActual = subasta.Estado.ToString(), fechaFin = subasta.FechaFin });
        }

        if (subasta.VendedorId == command.UsuarioId)
        {
            await LanzarRechazoAsync(command.SubastaId, command.UsuarioId, "El vendedor no puede pujar en su propia subasta.", command.Monto);
        }

        var pujaLiderAnterior = subasta.Pujas
            .OrderByDescending(p => p.Monto)
            .FirstOrDefault();

        if (pujaLiderAnterior?.CompradorId == command.UsuarioId)
        {
            await LanzarRechazoAsync(command.SubastaId, command.UsuarioId, "Ya eres el líder actual de esta subasta. No puedes superarte a ti mismo.", command.Monto);
        }

        var montoMinimoRequerido = pujaLiderAnterior is null
            ? subasta.PrecioBase
            : pujaLiderAnterior.Monto + subasta.IncrementoMinimo;

        if (command.Monto < montoMinimoRequerido)
        {
            await LanzarRechazoAsync(command.SubastaId, command.UsuarioId, $"El monto ofertado (${command.Monto}) debe ser al menos de ${montoMinimoRequerido}.", command.Monto, new { montoMinimoRequerido });
        }

        var billeteraNuevoOfertante = await _walletRepository.GetByUserIdAsync(command.UsuarioId);
        if (billeteraNuevoOfertante == null)
        {
            await LanzarRechazoAsync(command.SubastaId, command.UsuarioId, "El usuario no posee una billetera virtual activa.", command.Monto);
        }

        if (billeteraNuevoOfertante!.SaldoDisponible < command.Monto)
        {
            await LanzarRechazoAsync(command.SubastaId, command.UsuarioId, $"Saldo insuficiente en la billetera. Disponible: ${billeteraNuevoOfertante.SaldoDisponible}, Requerido: ${command.Monto}.", command.Monto, new { saldoDisponible = billeteraNuevoOfertante.SaldoDisponible });
        }

        if (pujaLiderAnterior is not null)
        {
            var billeteraLiderAnterior = await _walletRepository.GetByUserIdAsync(pujaLiderAnterior.CompradorId);

            if (billeteraLiderAnterior is not null)
            {
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
        }

        billeteraNuevoOfertante.RetenerFondos(command.Monto);
        await _ledgerRepository.AddAsync(new TransaccionLedger
        {
            BilleteraId = billeteraNuevoOfertante.Id,
            Tipo = TipoTransaccion.Retencion,
            Monto = command.Monto,
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
                }
            );
        }

        // Confirmamos de manera atómica (puja + billetera + ledger + auditoría si aplicó)
        // La colisión de concurrencia optimista (DbUpdateConcurrencyException) es capturada
        // globalmente por el ExceptionMiddleware para retornar HTTP 409 y registrar AuditoriaLog.
        await _unitOfWork.SaveChangesAsync();

        var response = new PujaResponseDto(
            nuevaPuja.Id,
            subasta.Id,
            command.UsuarioId,
            command.Monto,
            nuevaPuja.FechaPuja,
            subasta.FechaFin,
            antiSnipingActivado);

        // Notificación en tiempo real por SignalR
        await _notifier.NotificarNuevaPujaAsync(response);

        return response;
    }

    private async Task LanzarRechazoAsync(int subastaId, int usuarioId, string motivo, decimal? monto, object? detalleAdicional = null)
    {
        await _auditService.RegistrarYConfirmarEventoAsync(
            entidad: "Subasta",
            entidadId: subastaId,
            accion: "PUJA_RECHAZADA",
            usuarioId: usuarioId,
            detalle: new
            {
                motivo,
                montoOfertado = monto,
                detalleAdicional
            }
        );
        throw new DomainException(motivo);
    }
}