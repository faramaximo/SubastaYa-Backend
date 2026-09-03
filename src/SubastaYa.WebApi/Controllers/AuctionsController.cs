using Microsoft.AspNetCore.Mvc;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using System.Threading.Tasks;

namespace SubastaYa.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionsController : ControllerBase
    {
        private readonly IAuctionService _auctionService;

        public AuctionsController(IAuctionService auctionService)
        {
            _auctionService = auctionService;
        }

        // 🔴 ESTE DEBE SER EL ÚNICO [HttpGet] SIN RUTA (Para la lista y filtros)
        [HttpGet]
        public async Task<IActionResult> GetAuctions(
            [FromQuery] int? estado,
            [FromQuery] int? categoriaId,
            [FromQuery] decimal? precioMin,
            [FromQuery] decimal? precioMax,
            [FromQuery] string? busqueda,
            [FromQuery] string orderBy = "menor-tiempo")
        {
            //El controlador llama al servicio
            var subastas = await _auctionService.ObtenerSubastasAsync(estado, categoriaId, precioMin, precioMax, busqueda, orderBy);
            return Ok(subastas);
        }

        // [HttpGet] con ruta para traer una sola subasta por ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuctionById(int id)
        {
            var subasta = await _auctionService.GetAuctionByIdAsync(id);
            if (subasta == null) return NotFound();
            return Ok(subasta);
        }

        // [HttpPost] para crear subastas (Módulo 2)
        [HttpPost]
        public async Task<IActionResult> CreateAuction([FromBody] CreateAuctionDto dto)
        {
            var subasta = await _auctionService.CreateAuctionAsync(dto);
            return CreatedAtAction(nameof(GetAuctionById), new { id = subasta.Id }, subasta);
        }
    }
}