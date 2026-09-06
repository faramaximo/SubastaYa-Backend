using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Application.UseCases.Auctions.Commands
{
    // Solo transporta datos. No tiene lógica.
    public record CreateBidCommand(int SubastaId, int CompradorId, decimal Monto);
}