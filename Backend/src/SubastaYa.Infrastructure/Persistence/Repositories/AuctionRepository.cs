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

        public async Task<List<Subasta>> ObtenerProgramadasParaIniciarAsync(DateTime fecha, CancellationToken cancellationToken = default)
        {
            return await _context.Subastas
                .Where(s => s.Estado == EstadoSubasta.Programada && s.FechaInicio <= fecha && s.FechaFin > fecha)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<int>> ObtenerIdsVencidasAsync(DateTime fecha, CancellationToken cancellationToken = default)
        {
            return await _context.Subastas
                .Where(s => (s.Estado == EstadoSubasta.Activa || s.Estado == EstadoSubasta.Programada) && s.FechaFin <= fecha)
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);
        }
    }
}