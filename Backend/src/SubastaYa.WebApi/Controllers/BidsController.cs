using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Bids.Commands;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BidsController : ControllerBase
{
    private readonly RegisterBidCommandHandler _bidHandler;

    public BidsController(RegisterBidCommandHandler bidHandler)
    {
        _bidHandler = bidHandler;
    }

    [HttpPost]
    public async Task<IActionResult> RegistrarPuja([FromBody] RegistrarPujaDto dto)
    {
        var usuarioId = dto.UsuarioId ?? ObtenerUsuarioId();
        var resultado = await _bidHandler.Handle(new RegisterBidCommand(dto.SubastaId, usuarioId, dto.Monto));
        return Ok(resultado);
    }

    private int ObtenerUsuarioId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
}