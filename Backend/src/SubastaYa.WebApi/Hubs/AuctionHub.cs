using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace SubastaYa.WebApi.Hubs
{
    public class AuctionHub : Hub
    {
        // Permite que el cliente se una a la sala específica de una subasta
        public async Task UnirseASubasta(string subastaId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"subasta-{subastaId}");
        }

        // Permite que el cliente abandone la sala al salir de la pantalla
        public async Task SalirDeSubasta(string subastaId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"subasta-{subastaId}");
        }

    }
}
