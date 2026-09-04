using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubastaYa.Infrastructure.Data;

namespace SubastaYa.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly SubastaYaDbContext _context;

        public UsuariosController(SubastaYaDbContext context)
        {
            _context = context;
        }

        // GET: api/usuarios/5/publicaciones
        [HttpGet("{id}/publicaciones")]
        public async Task<IActionResult> GetMisPublicaciones(int id)
        {
            var publicaciones = await _context.Subastas
                .Where(s => s.VendedorId == id)
                .Select(s => new {
                    s.Id,
                    s.Titulo,
                    s.UrlImagen,
                    s.Estado,
                    s.FechaFin,
                    PrecioBase = s.PrecioBase,
                    // Calculamos la oferta más alta directamente en la base de datos
                    OfertaMasAlta = _context.Pujas.Where(p => p.SubastaId == s.Id).Max(p => (decimal?)p.Monto) ?? 0,
                    CantidadPujas = _context.Pujas.Count(p => p.SubastaId == s.Id)
                })
                .OrderByDescending(s => s.FechaFin)
                .ToListAsync();

            return Ok(publicaciones);
        }

        // GET: api/usuarios/5/pujas
        [HttpGet("{id}/pujas")]
        public async Task<IActionResult> GetMisPujas(int id)
        {
            // Buscamos subastas donde el usuario haya metido al menos una puja
            var participaciones = await _context.Subastas
                .Where(s => _context.Pujas.Any(p => p.SubastaId == s.Id && p.CompradorId == id))
                .Select(s => new {
                    s.Id,
                    s.Titulo,
                    s.UrlImagen,
                    s.Estado,
                    s.FechaFin,
                    // Buscamos cuál fue mi oferta más alta
                    MiMaximaPuja = _context.Pujas.Where(p => p.SubastaId == s.Id && p.CompradorId == id).Max(p => p.Monto),
                    // Buscamos la oferta ganadora general
                    OfertaGanadora = _context.Pujas.Where(p => p.SubastaId == s.Id).Max(p => (decimal?)p.Monto) ?? 0
                })
                .OrderByDescending(s => s.FechaFin)
                .ToListAsync();

            return Ok(participaciones);
        }
    }
}