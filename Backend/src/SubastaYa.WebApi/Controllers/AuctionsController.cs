using Microsoft.AspNetCore.Mvc;
using SubastaYa.Application.UseCases.Auctions.Commands;
using SubastaYa.Application.UseCases.Auctions.Handlers;
using SubastaYa.Application.UseCases.Auctions.Queries;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuctionsController : ControllerBase
{
    private readonly SearchAuctionsQueryHandler _searchHandler;
    private readonly GetAuctionByIdQueryHandler _getByIdHandler;
    private readonly CreateAuctionCommandHandler _createHandler;

    public AuctionsController(
        SearchAuctionsQueryHandler searchHandler,
        GetAuctionByIdQueryHandler getByIdHandler,
        CreateAuctionCommandHandler createHandler)
    {
        _searchHandler = searchHandler;
        _getByIdHandler = getByIdHandler;
        _createHandler = createHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuctions(
        [FromQuery] int? estado,
        [FromQuery] int? categoriaId,
        [FromQuery] decimal? precioMin,
        [FromQuery] decimal? precioMax,
        [FromQuery] string? busqueda,
        [FromQuery] string orderBy = "menor-tiempo")
    {
        var query = new SearchAuctionsQuery(estado, categoriaId, precioMin, precioMax, busqueda, orderBy);
        return Ok(await _searchHandler.Handle(query));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAuctionById(int id)
    {
        var subasta = await _getByIdHandler.Handle(new GetAuctionByIdQuery(id));
        return subasta is null ? NotFound() : Ok(subasta);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAuction([FromBody] CreateAuctionCommand command)
    {
        var subastaId = await _createHandler.Handle(command);
        return CreatedAtAction(nameof(GetAuctionById), new { id = subastaId }, new { id = subastaId });
    }
}
