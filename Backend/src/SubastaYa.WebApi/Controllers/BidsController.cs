using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Bids.Commands;
using SubastaYa.WebApi.Hubs;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize]
public class BidsController : ControllerBase
{
    private readonly RegisterBidCommandHandler _registerBidHandler;
    private readonly IHubContext<AuctionHub> _hubContext;

    public BidsController(
        RegisterBidCommandHandler registerBidHandler,
        IHubContext<AuctionHub> hubContext)
    {
        _registerBidHandler = registerBidHandler;
        _hubContext = hubContext;
    }

    [HttpPost]
    public async Task<IActionResult> RegistrarPuja([FromBody] RegistrarPujaDto dto)
    {
        // Resolver usuarioId: preferir el claim del token si está presente
        int? usuarioId = dto.UsuarioId;
        if (!usuarioId.HasValue)
        {
            var claim = HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier) ?? HttpContext.User?.FindFirst("id") ?? HttpContext.User?.FindFirst("sub");
            if (claim != null && int.TryParse(claim.Value, out var parsed))
                usuarioId = parsed;
        }

        if (!usuarioId.HasValue)
            return Unauthorized(new { error = "Usuario no autenticado. Iniciá sesión para pujar." });

        var command = new RegisterBidCommand(dto.SubastaId, usuarioId.Value, dto.Monto);
        var resultado = await _registerBidHandler.Handle(command);

        await _hubContext.Clients
            .Group($"subasta-{dto.SubastaId}")
            .SendAsync("NuevaPujaRegistrada", resultado);

        return Ok(resultado);
    }
}
