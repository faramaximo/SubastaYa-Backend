using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Bids.Commands;
using SubastaYa.WebApi.Hubs;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        var command = new RegisterBidCommand(dto.SubastaId, dto.UsuarioId, dto.Monto);
        var resultado = await _registerBidHandler.Handle(command);

        await _hubContext.Clients
            .Group($"subasta-{dto.SubastaId}")
            .SendAsync("NuevaPujaRegistrada", resultado);

        return Ok(resultado);
    }
}
