using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Auctions.Commands;
using SubastaYa.Application.UseCases.Auctions.Handlers;
using SubastaYa.Application.UseCases.Auctions.Queries;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Route("api/v1/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly SearchAuctionsQueryHandler _searchHandler;
    private readonly GetAuctionByIdQueryHandler _getByIdHandler;
    private readonly CreateAuctionCommandHandler _createHandler;

    public AuctionsController(SearchAuctionsQueryHandler searchHandler, GetAuctionByIdQueryHandler getByIdHandler, CreateAuctionCommandHandler createHandler)
    {
        _searchHandler = searchHandler;
        _getByIdHandler = getByIdHandler;
        _createHandler = createHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AuctionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuctions([FromQuery] int? estado, [FromQuery] int? categoriaId,
        [FromQuery] decimal? precioMin, [FromQuery] decimal? precioMax, [FromQuery] string? busqueda,
        [FromQuery] string orderBy = "menor-tiempo") =>
        Ok(await _searchHandler.Handle(new SearchAuctionsQuery(estado, categoriaId, precioMin, precioMax, busqueda, orderBy)));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AuctionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuctionById(int id)
    {
        var subasta = await _getByIdHandler.Handle(new GetAuctionByIdQuery(id));
        return subasta is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Recurso no encontrado", detail: "La subasta especificada no existe.")
            : Ok(subasta);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateAuction([FromBody] CreateAuctionDto dto)
    {
        var vendedorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var subastaId = await _createHandler.Handle(new CreateAuctionCommand(vendedorId, dto.CategoriaId, dto.Titulo,
            dto.Descripcion, dto.UrlImagen, dto.PrecioBase, dto.IncrementoMinimo, dto.FechaInicio, dto.FechaFin));
        return CreatedAtAction(nameof(GetAuctionById), new { id = subastaId }, new { id = subastaId });
    }
}