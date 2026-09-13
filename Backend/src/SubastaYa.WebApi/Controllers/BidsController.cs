using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Bids.Commands;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize]
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

        return Ok(resultado);
    }
}