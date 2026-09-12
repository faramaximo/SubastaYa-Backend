using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Enums;
using SubastaYa.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace SubastaYa.Application.UseCases.Bids.Commands;

public class RegisterBidCommandHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ILedgerRepository _ledgerRepository;

    public RegisterBidCommandHandler(
        IUnitOfWork unitOfWork,
        IAuctionRepository auctionRepository,
        IWalletRepository walletRepository,
        ILedgerRepository ledgerRepository)
    {
        _unitOfWork = unitOfWork;
        _auctionRepository = auctionRepository;
        _walletRepository = walletRepository;
        _ledgerRepository = ledgerRepository;
    }


    //Valida reglas de negocio

    public async Task<PujaResponseDto> Handle(RegisterBidCommand command)
    {
        var subasta = await _auctionRepository.GetByIdWithBidsAsync(command.SubastaId)
            ?? throw new DomainException("La subasta especificada no existe.");

        if (subasta.Estado != EstadoSubasta.Activa || subasta.FechaFin <= DateTime.UtcNow)
            throw new DomainException("La subasta no se encuentra activa o ya ha finalizado.");

        if (subasta.VendedorId == command.UsuarioId)
            throw new DomainException("El vendedor no puede pujar en su propia subasta.");

        var pujaLiderAnterior = subasta.Pujas
            .OrderByDescending(p => p.Monto)
            .FirstOrDefault();

        var montoMinimoRequerido = pujaLiderAnterior is null
            ? subasta.PrecioBase
            : pujaLiderAnterior.Monto + subasta.IncrementoMinimo;

        if (command.Monto < montoMinimoRequerido)
            throw new DomainException($"El monto ofertado (${command.Monto}) debe ser al menos de ${montoMinimoRequerido}.");

        var billeteraNuevoOfertante = await _walletRepository.GetByUserIdAsync(command.UsuarioId)
            ?? throw new DomainException("El usuario no posee una billetera virtual activa.");

        if (billeteraNuevoOfertante.SaldoDisponible < command.Monto)
            throw new DomainException($"Saldo insuficiente en la billetera. Disponible: ${billeteraNuevoOfertante.SaldoDisponible}, Requerido: ${command.Monto}.");

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

        try 
        { 
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException("Alguien más realizó una puja al mismo tiempo. Actualizá la subasta y volvé a intentarlo.");
        }
       

        return new PujaResponseDto(
            nuevaPuja.Id,
            subasta.Id,
            command.UsuarioId,
            command.Monto,
            nuevaPuja.FechaPuja,
            subasta.FechaFin,
            subasta.FechaFin > fechaFinAntes);
    }
}
