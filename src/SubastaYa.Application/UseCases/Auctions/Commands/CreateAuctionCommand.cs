using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Application.UseCases.Auctions.Commands
{
    public record CreateAuctionCommand(
        int VendedorId,
        int CategoriaId,
        string Titulo,
        string Descripcion,
        string UrlImagen, 
        decimal PrecioBase,
        decimal IncrementoMinimo,
        DateTime FechaFin
    );
}
