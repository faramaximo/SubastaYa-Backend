using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SubastaYa.Application.DTOs;
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

        Console.WriteLine($"\n---> DEBUG: Estado recibido desde Swagger: {estado} <--- \n");
        var query = new SearchAuctionsQuery(estado, categoriaId, precioMin, precioMax, busqueda, orderBy);
        return Ok(await _searchHandler.Handle(query));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAuctionById(int id)
    {
        var subasta = await _getByIdHandler.Handle(new GetAuctionByIdQuery(id));
        if (subasta is null) return NotFound();

        // Exponemos el token de concurrencia como ETag para que el cliente
        // pueda enviarlo de vuelta en If-Match al pujar (contrato REST completo).
        if (subasta.Version.Length > 0)
        {
            Response.Headers["ETag"] = $"\"{Convert.ToBase64String(subasta.Version)}\"";
        }

        return Ok(subasta);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateAuction([FromBody] CreateAuctionDto dto)
    {
        var vendedorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new CreateAuctionCommand(
            vendedorId,
            dto.CategoriaId,
            dto.Titulo,
            dto.Descripcion,
            dto.UrlImagen,
            dto.PrecioBase,
            dto.IncrementoMinimo,
            dto.FechaInicio,
            dto.FechaFin
        );
        var subastaId = await _createHandler.Handle(command);
        return CreatedAtAction(nameof(GetAuctionById), new { id = subastaId }, new { id = subastaId });
    }
}
