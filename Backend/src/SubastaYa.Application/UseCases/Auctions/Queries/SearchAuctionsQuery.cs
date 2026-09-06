using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Application.UseCases.Auctions.Queries
{
    public record SearchAuctionsQuery(
        int? Estado,
        int? CategoriaId,
        decimal? PrecioMin,
        decimal? PrecioMax,
        string? Busqueda,
        string OrderBy
    );
}
