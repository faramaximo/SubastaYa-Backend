using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.Services;
using SubastaYa.WebApi.Hubs;

namespace SubastaYa.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BidsController : ControllerBase
    {
        private readonly IBidService _bidService;
        private readonly IHubContext<AuctionHub> _hubContext;

        public BidsController(IBidService bidService, IHubContext<AuctionHub> hubContext)
        {
            _bidService = bidService;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarPuja([FromBody] RegistrarPujaDto dto)
        {
            // 1. Ejecutar la lógica de negocio
            var resultado = await _bidService.RegistrarPujaAsync(dto);

            // 2. Notificar en TIEMPO REAL a todos los clientes conectados a la sala de esta subasta
            await _hubContext.Clients
                .Group($"subasta-{dto.SubastaId}")
                .SendAsync("NuevaPujaRegistrada", resultado);

            return Ok(resultado);
        }
    }
}

