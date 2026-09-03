using System.Collections.Generic;
using System.Threading.Tasks;
using SubastaYa.Application.DTOs;
using SubastaYa.Domain.Enums;

namespace SubastaYa.Application.Interfaces;

public interface IAuctionService //es un contrato que define los métodos que se implementarán en la clase AuctionService,
                                 //que se encargará de manejar la lógica de negocio relacionada con las subastas.
{
    Task<IEnumerable<AuctionDto>> ObtenerSubastasAsync(int? estado, int? categoriaId, decimal? precioMin, decimal? precioMax, string? busqueda, string orderBy);
    Task<IEnumerable<AuctionDto>> GetAllAuctionsAsync(EstadoSubasta? estado, int? categoriaId);
    Task<AuctionDto?> GetAuctionByIdAsync(int id);
    Task<AuctionDto> CreateAuctionAsync(CreateAuctionDto createAuctionDto);
}
