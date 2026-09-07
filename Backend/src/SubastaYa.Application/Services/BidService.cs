using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.DTOs;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Enums;
using SubastaYa.Domain.Exceptions;
using SubastaYa.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Application.Services
{
    public class BidService : IBidService
    {
        private readonly SubastaYaDbContext _context;

        public BidService(SubastaYaDbContext context)
        {
            _context = context;
        }

        public async Task<PujaResponseDto> RegistrarPujaAsync(RegistrarPujaDto dto)
        {
            // 1. Obtener Subasta
            var subasta = await _context.Subastas
                .Include(s => s.Pujas)
                .FirstOrDefaultAsync(s => s.Id == dto.SubastaId)
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
            var pujaLiderAnterior = subasta.Pujas
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

            // 3. Obtener Billetera
            var billeteraNuevoOfertante = await _context.Billeteras
                .FirstOrDefaultAsync(b => b.UsuarioId == dto.UsuarioId)
                ?? throw new DomainException("El usuario no posee una billetera virtual activa.");

            decimal saldoDisponible = billeteraNuevoOfertante.SaldoTotal - billeteraNuevoOfertante.SaldoRetenido;
            if (saldoDisponible < dto.Monto)
            {
                throw new DomainException($"Saldo insuficiente en la billetera. Disponible: ${saldoDisponible}, Requerido: ${dto.Monto}.");
            }

            // 4. Liberar Saldo al Líder Anterior
            if (pujaLiderAnterior != null)
            {
                var billeteraLiderAnterior = await _context.Billeteras
                    .FirstOrDefaultAsync(b => b.UsuarioId == pujaLiderAnterior.Id);

                if (billeteraLiderAnterior != null)
                {
                    billeteraLiderAnterior.SaldoRetenido -= pujaLiderAnterior.Monto;

                    _context.TransaccionesLedger.Add(new TransaccionLedger
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
            billeteraNuevoOfertante.SaldoRetenido += dto.Monto;

            _context.TransaccionesLedger.Add(new TransaccionLedger
            {
                BilleteraId = billeteraNuevoOfertante.Id,
                Tipo = TipoTransaccion.Retencion,
                Monto = dto.Monto,
                Fecha = DateTime.UtcNow,
                SubastaId = subasta.Id
            });

            // 6. Regla Anti-Sniping
            bool tiempoExtendido = false;
            var tiempoRestante = subasta.FechaFin - DateTime.UtcNow;
            if (tiempoRestante <= TimeSpan.FromMinutes(2))
            {
                subasta.FechaFin = subasta.FechaFin.AddMinutes(2);
                tiempoExtendido = true;
            }

            // 7. Crear la Puja
            var nuevaPuja = new Puja
            {
                SubastaId = dto.SubastaId,
                CompradorId = dto.UsuarioId,
                Monto = dto.Monto,
                FechaPuja = DateTime.UtcNow
            };

            _context.Pujas.Add(nuevaPuja);

            // 8. Guardar en Base de Datos
            await _context.SaveChangesAsync();

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