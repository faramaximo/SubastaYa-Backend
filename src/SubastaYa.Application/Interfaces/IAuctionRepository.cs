using SubastaYa.Domain.Entities;

public interface IAuctionRepository
{
    Task<Subasta?> ObtenerPorIdAsync(int id);
    Task AgregarAsync(Subasta subasta);
    // Para la lectura compleja, podemos definir un método específico:
    Task<IEnumerable<Subasta>> BuscarSubastasAsync(int? estado, int? categoriaId, decimal? precioMin, decimal? precioMax, string? busqueda, string orderBy);
}