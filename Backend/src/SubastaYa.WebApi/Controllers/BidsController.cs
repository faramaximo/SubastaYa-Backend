using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Bids.Commands;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BidsController : ControllerBase
{
    private readonly RegisterBidCommandHandler _registerBidHandler;

    public BidsController(RegisterBidCommandHandler registerBidHandler)
    {
        _registerBidHandler = registerBidHandler;
    }

    [HttpPost]
    public async Task<IActionResult> RegistrarPuja([FromBody] RegistrarPujaDto dto)
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var command = new RegisterBidCommand(dto.SubastaId, usuarioId, dto.Monto);
        var resultado = await _registerBidHandler.Handle(command);

        return Ok(resultado);
    }
}