using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Bids.Commands;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/auctions/{auctionId:int}/bids")]
public class BidsController : ControllerBase
{
    private readonly RegisterBidCommandHandler _registerBidHandler;

    public BidsController(RegisterBidCommandHandler registerBidHandler) => _registerBidHandler = registerBidHandler;

    /// <summary>Crea una puja para la subasta indicada.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PujaResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateBid(int auctionId, [FromBody] CreateBidRequestDto dto)
    {
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var resultado = await _registerBidHandler.Handle(new RegisterBidCommand(auctionId, usuarioId, dto.Monto!.Value));

        return Created($"/api/v1/auctions/{auctionId}/bids/{resultado.Id}", resultado);
    }
}

public sealed class CreateBidRequestDto
{
    [Required]
    public decimal? Monto { get; init; }
}