using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Auctions.Commands;
using SubastaYa.Application.UseCases.Auctions.Handlers;
using SubastaYa.Application.UseCases.Auctions.Queries;
using SubastaYa.Application.UseCases.Bids.Commands;

namespace SubastaYa.WebApi.Controllers;

public record AuctionBidRequestDto(decimal Monto, int? UsuarioId = null);

[ApiController]
[Route("api/[controller]")]
public class AuctionsController : ControllerBase
{
    private readonly SearchAuctionsQueryHandler _searchHandler;
    private readonly GetAuctionByIdQueryHandler _getByIdHandler;
    private readonly CreateAuctionCommandHandler _createHandler;
    private readonly RegisterBidCommandHandler _bidHandler;

    public AuctionsController(
        SearchAuctionsQueryHandler searchHandler,
        GetAuctionByIdQueryHandler getByIdHandler,
        CreateAuctionCommandHandler createHandler,
        RegisterBidCommandHandler bidHandler)
    {
        _searchHandler = searchHandler;
        _getByIdHandler = getByIdHandler;
        _createHandler = createHandler;
        _bidHandler = bidHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuctions([FromQuery] SearchAuctionsQuery query) =>
        Ok(await _searchHandler.Handle(query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAuctionById(int id)
    {
        var subasta = await _getByIdHandler.Handle(new GetAuctionByIdQuery(id));
        return subasta is null ? NotFound() : Ok(subasta);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAuction([FromBody] CreateAuctionCommand command)
    {
        var id = await _createHandler.Handle(command);
        return CreatedAtAction(nameof(GetAuctionById), new { id }, new { id });
    }

    [HttpPost("{id:int}/bids")]
    public async Task<IActionResult> RegistrarPujaParaSubasta(int id, [FromBody] AuctionBidRequestDto dto)
    {
        var usuarioId = dto.UsuarioId ?? ObtenerUsuarioId();
        var resultado = await _bidHandler.Handle(new RegisterBidCommand(id, usuarioId, dto.Monto));
        return Ok(resultado);
    }

    private int ObtenerUsuarioId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
}
