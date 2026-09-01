using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Enums;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly IAuctionService _auctionService;

    public AuctionsController(IAuctionService auctionService)
    {
        _auctionService = auctionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuctionDto>>> GetAuctions([FromQuery] EstadoSubasta? estado, [FromQuery] int? categoriaId)
    {
        var auctions = await _auctionService.GetAllAuctionsAsync(estado, categoriaId);
        return Ok(auctions);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionById(int id)
    {
        var auction = await _auctionService.GetAuctionByIdAsync(id);

        if (auction == null)
            return NotFound();

        return Ok(auction);
    }

    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction([FromBody] CreateAuctionDto createAuctionDto)
    {
        // Reglas de negocio UI validaciones extra
        if (createAuctionDto.FechaFin <= createAuctionDto.FechaInicio)
            return BadRequest("La fecha de finalización debe ser posterior a la de inicio.");

        if (createAuctionDto.PrecioBase <= 0 || createAuctionDto.IncrementoMinimo <= 0)
            return BadRequest("El precio base y el incremento mínimo deben ser positivos.");

        var createdAuction = await _auctionService.CreateAuctionAsync(createAuctionDto);
        return CreatedAtAction(nameof(GetAuctionById), new { id = createdAuction.Id }, createdAuction);
    }

    // Endpoint placeholder (A implementar cuando el caso de uso del 'FairPlay/Escrow' esté listo)
    [HttpPost("{id}/bids")]
    public async Task<IActionResult> PlaceBid(int id, [FromBody] CreateBidDto bidDto)
    {
        // Aquí se implementará la compleja lógica de bloqueo atómico de billetera y Anti-Sniping
        return await Task.FromResult(Ok(new { message = "Lógica de puja atómica pendiente de implementación", subastaId = id }));
    }
}
