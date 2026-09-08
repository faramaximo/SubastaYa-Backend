using SubastaYa.Application.DTOs;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Enums;
using SubastaYa.Domain.Exceptions;
using SubastaYa.Application.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SubastaYa.Application.Services
{
    public class BidService : IBidService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuctionRepository _auctionRepository;
        private readonly IWalletRepository _walletRepository;
        private readonly ILedgerRepository _ledgerRepository;

        public BidService(
            IUnitOfWork unitOfWork,
            IAuctionRepository auctionRepository,
            IWalletRepository walletRepository,
            ILedgerRepository ledgerRepository)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _auctionRepository = auctionRepository ?? throw new ArgumentNullException(nameof(auctionRepository));
            _walletRepository = walletRepository ?? throw new ArgumentNullException(nameof(walletRepository));
            _ledgerRepository = ledgerRepository ?? throw new ArgumentNullException(nameof(ledgerRepository));
        }

        public async Task<PujaResponseDto> RegistrarPujaAsync(RegistrarPujaDto dto)
        {
            // 1. Obtener Subasta (con pujas)
            var subasta = await _auctionRepository.GetByIdWithBidsAsync(dto.SubastaId)
                ?? throw new DomainException("La subasta especificada no existe.");

            if (subasta.Estado != EstadoSubasta.Activa || subasta.FechaFin <= DateTime.UtcNow)
            {
                throw new DomainException("La subasta no se encuentra activa o ya ha finalizado.");
            }

            if (subasta.VendedorId == dto.UsuarioId)
            {
                throw new DomainException("El vendedor no puede pujar en su propia subasta.");
            }

            // 2. Determinar Puja Líder y Precio
            var pujaLiderAnterior = subasta.Pujas?
                .OrderByDescending(p => p.Monto)
                .FirstOrDefault();

            decimal precioActual = pujaLiderAnterior != null ? pujaLiderAnterior.Monto : subasta.PrecioBase;
            decimal montoMinimoRequerido = pujaLiderAnterior != null
                ? precioActual + subasta.IncrementoMinimo
                : subasta.PrecioBase;

            if (dto.Monto < montoMinimoRequerido)
            {
                throw new DomainException($"El monto ofertado (${dto.Monto}) debe ser al menos de ${montoMinimoRequerido}.");
            }

            // 3. Obtener Billetera del nuevo ofertante
            var billeteraNuevoOfertante = await _walletRepository.GetByUserIdAsync(dto.UsuarioId)
                ?? throw new DomainException("El usuario no posee una billetera virtual activa.");

            decimal saldoDisponible = billeteraNuevoOfertante.SaldoTotal - billeteraNuevoOfertante.SaldoRetenido;
            if (saldoDisponible < dto.Monto)
            {
                throw new DomainException($"Saldo insuficiente en la billetera. Disponible: ${saldoDisponible}, Requerido: ${dto.Monto}.");
            }

            // 4. Liberar Saldo al Líder Anterior (si existe)
            if (pujaLiderAnterior != null)
            {
                // BUG FIX: usar CompradorId para buscar la billetera del líder anterior
                var billeteraLiderAnterior = await _walletRepository.GetByUserIdAsync(pujaLiderAnterior.CompradorId);

                if (billeteraLiderAnterior != null)
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

            // 5. Retener Saldo al Nuevo Ofertante
            billeteraNuevoOfertante.RetenerFondos(dto.Monto);

            await _ledgerRepository.AddAsync(new TransaccionLedger
            {
                BilleteraId = billeteraNuevoOfertante.Id,
                Tipo = TipoTransaccion.Retencion,
                Monto = dto.Monto,
                Fecha = DateTime.UtcNow,
                SubastaId = subasta.Id
            });

            // 6. Registrar la puja dentro de la entidad Subasta (encapsula validaciones y anti-sniping)
            var fechaFinAntes = subasta.FechaFin;
            subasta.RegistrarPuja(dto.UsuarioId, dto.Monto);
            bool tiempoExtendido = subasta.FechaFin > fechaFinAntes;

            // 7. Persistir cambios a través de UnitOfWork
            await _unitOfWork.SaveChangesAsync();

            // Obtener la puja recién agregada (la entidad Subasta la creó)
            var nuevaPuja = subasta.Pujas.OrderByDescending(p => p.FechaPuja).FirstOrDefault()!;

            return new PujaResponseDto(
                nuevaPuja.Id,
                subasta.Id,
                dto.UsuarioId,
                dto.Monto,
                nuevaPuja.FechaPuja,
                subasta.FechaFin,
                tiempoExtendido
            );
        }
    }
}