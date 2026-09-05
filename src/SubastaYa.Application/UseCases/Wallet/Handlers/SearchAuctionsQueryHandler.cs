using SubastaYa.Application.DTOs;

public class SearchAuctionsQueryHandler
{
    private readonly IAuctionRepository _subastas;

    public SearchAuctionsQueryHandler(IAuctionRepository subastas)
    {
        _subastas = subastas;
    }

    public async Task<IEnumerable<AuctionDto>> Handle(SearchAuctionsQuery query)
    {
        // El repositorio ejecuta los .Where() y el OrderBy que tenías, 
        // pero asegurate de que en la implementación del repositorio uses .AsNoTracking()
        // para no cargar la memoria con fotocopias innecesarias[cite: 1].
        var subastas = await _subastas.BuscarSubastasAsync(
            query.Estado,
            query.CategoriaId,
            query.PrecioMin,
            query.PrecioMax,
            query.Busqueda,
            query.OrderBy
        );

        return subastas.Select(s => new AuctionDto
        {
            Id = s.Id,
            Titulo = s.Titulo,
            CategoriaNombre = s.Categoria?.Nombre ?? "",
            UrlImagen = s.UrlImagen,
            OfertaMasAlta = s.Pujas.Any() ? s.Pujas.Max(p => p.Monto) : s.PrecioBase,
            FechaInicio = s.FechaInicio,
            FechaFin = s.FechaFin,
            CantidadOfertas = s.Pujas.Count,
            Estado = s.Estado
        });
    }
}