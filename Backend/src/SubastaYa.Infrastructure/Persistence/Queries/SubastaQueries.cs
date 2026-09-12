using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Enums;
using SubastaYa.Infrastructure.Data;

namespace SubastaYa.Infrastructure.Persistence.Queries
{
    public class SubastaQueries : ISubastaQueries
    {
        private readonly SubastaYaDbContext _ctx;

        public SubastaQueries(SubastaYaDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<List<PublicacionResumenDto>> GetMisPublicacionesAsync(int vendedorId)
        {
            return await _ctx.Subastas
                .AsNoTracking()
                .Where(s => s.VendedorId == vendedorId)
                // 1. ORDENAMOS ANTES de proyectar
                .OrderByDescending(s => s.FechaFin)
                // 2. Usamos s.Pujas (propiedad de navegación) en vez de _ctx.Pujas
                .Select(s => new PublicacionResumenDto(
                    s.Id,
                    s.Titulo,
                    s.UrlImagen,
                    s.Estado,
                    s.FechaFin,
                    s.PrecioBase,
                    s.Pujas.Max(p => (decimal?)p.Monto) ?? 0,
                    s.Pujas.Count
                ))
                .ToListAsync();
        }

        public async Task<List<ParticipacionResumenDto>> GetMisPujasAsync(int compradorId)
        {
            return await _ctx.Subastas
                .AsNoTracking()
                .Where(s => s.Pujas.Any(p => p.CompradorId == compradorId))
                // 1. ORDENAMOS ANTES de proyectar
                .OrderByDescending(s => s.FechaFin)
                .Select(s => new ParticipacionResumenDto(
                    s.Id,
                    s.Titulo,
                    s.UrlImagen,
                    s.Estado,
                    s.FechaFin,
                    // 2. Usamos s.Pujas y casteamos a (decimal?) de forma segura
                    s.Pujas.Where(p => p.CompradorId == compradorId).Max(p => (decimal?)p.Monto) ?? 0,
                    s.Pujas.Max(p => (decimal?)p.Monto) ?? 0
                ))
                .ToListAsync();
        }

        public async Task<IEnumerable<AuctionDto>> SearchAuctionsAsync(int? estado, int? categoriaId, decimal? precioMin, decimal? precioMax, string? busqueda, string orderBy)
        {
            var query = _ctx.Subastas.AsNoTracking().AsQueryable();

            // 1. FILTROS
            if (estado.HasValue)
            {
                query = query.Where(s => s.Estado == (SubastaYa.Domain.Enums.EstadoSubasta)estado.Value);
            }

            if (categoriaId.HasValue)
            {
                query = query.Where(s => s.CategoriaId == categoriaId.Value);
            }

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var termino = busqueda.Trim().ToLower();
                query = query.Where(s => s.Titulo.ToLower().Contains(termino) ||
                                         s.Descripcion.ToLower().Contains(termino));
            }

            if (precioMin.HasValue)
            {
                query = query.Where(s => (s.Pujas.Max(p => (decimal?)p.Monto) ?? s.PrecioBase) >= precioMin.Value);
            }

            if (precioMax.HasValue)
            {
                query = query.Where(s => (s.Pujas.Max(p => (decimal?)p.Monto) ?? s.PrecioBase) <= precioMax.Value);
            }

            // 2. ORDENAMIENTO
            query = orderBy switch
            {
                "mayor-tiempo" => query.OrderByDescending(s => s.FechaFin),
                "menor-puja" => query.OrderBy(s => s.Pujas.Max(p => (decimal?)p.Monto) ?? s.PrecioBase),
                "mayor-puja" => query.OrderByDescending(s => s.Pujas.Max(p => (decimal?)p.Monto) ?? s.PrecioBase),
                _ => query.OrderBy(s => s.FechaFin)
            };

            // 3. PROYECCIÓN DIRECTA A DTO
            return await query.Select(s => new AuctionDto
            {
                Id = s.Id,
                Titulo = s.Titulo,
                Descripcion = s.Descripcion,
                UrlImagen = s.UrlImagen,
                CategoriaNombre = s.Categoria.Nombre,
                PrecioBase = s.PrecioBase,
                IncrementoMinimo = s.IncrementoMinimo,
                OfertaMasAlta = s.Pujas.Max(p => (decimal?)p.Monto) ?? 0,
                CantidadOfertas = s.Pujas.Count(),
                FechaInicio = s.FechaInicio,
                FechaFin = s.FechaFin,
                Estado = s.Estado
            }).ToListAsync();
        }

    }
}