using Microsoft.AspNetCore.SignalR;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using SubastaYa.WebApi.Hubs;

namespace SubastaYa.WebApi.Hubs
{
    public class SignalRAuctionNotifier : IAuctionNotifier
    {
        private readonly IHubContext<AuctionHub> _hubContext;

        public SignalRAuctionNotifier(IHubContext<AuctionHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotificarNuevaPujaAsync(PujaResponseDto dto)
        {
            await _hubContext.Clients.Group($"subasta-{dto.SubastaId}").SendAsync("NuevaPujaRegistrada", dto);
            await _hubContext.Clients.All.SendAsync("SubastaActualizada", dto);
        }

        public async Task NotificarSubastaFinalizadaAsync(int subastaId, int? ganadorId, decimal montoFinal)
        {
            if (ganadorId.HasValue)
            {
                await _hubContext.Clients.Group($"subasta-{subastaId}")
                    .SendAsync("SubastaCerrada", new { SubastaId = subastaId, GanadorId = ganadorId.Value, MontoFinal = montoFinal });
            }
            else
            {
                await _hubContext.Clients.Group($"subasta-{subastaId}")
                    .SendAsync("SubastaDesierta", subastaId);
            }

            await _hubContext.Clients.All.SendAsync("SubastaActualizada");
        }

        public async Task NotificarSubastaIniciadaAsync(int subastaId)
        {
            await _hubContext.Clients.Group($"subasta-{subastaId}").SendAsync("SubastaIniciada", subastaId);
            await _hubContext.Clients.All.SendAsync("SubastaActualizada");
        }

        public async Task NotificarActualizacionCatalogoAsync()
        {
            await _hubContext.Clients.All.SendAsync("SubastaActualizada");
        }
    }
}