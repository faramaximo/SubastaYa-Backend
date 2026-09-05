using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Enums;
using SubastaYa.Infrastructure.Data;

namespace SubastaYa.Infrastructure.Persistence.Repositories
{
    public class AuctionRepository : IAuctionRepository
    {
        private readonly SubastaYaDbContext _context;

        public AuctionRepository(SubastaYaDbContext context)
        {
            _context = context;
        }

        public async Task<Subasta?> ObtenerPorIdAsync(int id)
        {
            return await _context.Subastas
                .Include(s => s.Categoria)
                .Include(s => s.Pujas)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task AgregarAsync(Subasta subasta)
        {
            await _context.Subastas.AddAsync(subasta);
        }

        public async Task<IEnumerable<Subasta>> BuscarSubastasAsync(int? estado, int? categoriaId, decimal? precioMin, decimal? precioMax, string? busqueda, string orderBy)
        {
            var query = _context.Subastas
                .Include(s => s.Categoria)
                .Include(s => s.Pujas)
                .AsNoTracking() // Fundamental para el rendimiento[cite: 1]
                .AsQueryable();

            if (estado.HasValue)
            {
                if (estado.Value == 2)
                    query = query.Where(s => s.Estado == EstadoSubasta.Finalizada || s.Estado == EstadoSubasta.Desierta);
                else
                    query = query.Where(s => s.Estado == (EstadoSubasta)estado.Value);
            }

            if (categoriaId.HasValue)
                query = query.Where(s => s.CategoriaId == categoriaId.Value);

            if (!string.IsNullOrEmpty(busqueda))
                query = query.Where(s => s.Titulo.Contains(busqueda));

            if (precioMin.HasValue)
                query = query.Where(s => (s.Pujas.Any() ? s.Pujas.Max(p => p.Monto) : s.PrecioBase) >= precioMin.Value);

            if (precioMax.HasValue)
                query = query.Where(s => (s.Pujas.Any() ? s.Pujas.Max(p => p.Monto) : s.PrecioBase) <= precioMax.Value);

            query = orderBy switch
            {
                "mayor-tiempo" => query.OrderByDescending(s => s.FechaFin),
                "menor-puja" => query.OrderBy(s => s.Pujas.Any() ? s.Pujas.Max(p => p.Monto) : s.PrecioBase),
                "mayor-puja" => query.OrderByDescending(s => s.Pujas.Any() ? s.Pujas.Max(p => p.Monto) : s.PrecioBase),
                _ => query.OrderBy(s => s.FechaFin)
            };

            return await query.ToListAsync();
        }
    }
}