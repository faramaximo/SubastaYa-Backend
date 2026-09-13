using SubastaYa.Application.DTOs;

namespace SubastaYa.Application.Interfaces
{
    public interface IAuctionNotifier
    {
        Task NotificarNuevaPujaAsync(PujaResponseDto dto);
        Task NotificarSubastaFinalizadaAsync(int subastaId, int? ganadorId, decimal montoFinal);
        Task NotificarSubastaIniciadaAsync(int subastaId);
        Task NotificarActualizacionCatalogoAsync();
    }
}