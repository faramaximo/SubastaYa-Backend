using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Application.UseCases.Auctions.Queries
{
    public record SearchAuctionsQuery(
        int? Estado = null,
        int? CategoriaId = null,
        decimal? PrecioMin = null,
        decimal? PrecioMax = null,
        string? Busqueda = null,
        string OrderBy = "menor-tiempo",
        int? Page = null,
        int? PageSize = null
    );
}
