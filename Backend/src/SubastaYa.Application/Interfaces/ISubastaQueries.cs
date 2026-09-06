using SubastaYa.Application.DTOs;

namespace SubastaYa.Application.Interfaces
{
    public interface ISubastaQueries
    {
        Task<List<PublicacionResumenDto>> GetMisPublicacionesAsync(int vendedorId);
        Task<List<ParticipacionResumenDto>> GetMisPujasAsync(int compradorId);
        Task<IEnumerable<AuctionDto>> SearchAuctionsAsync(int? estado, int? categoriaId, decimal? precioMin, decimal? precioMax, string? busqueda, string orderBy);

    }
}