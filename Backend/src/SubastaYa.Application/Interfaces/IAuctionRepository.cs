using SubastaYa.Domain.Entities;

namespace SubastaYa.Application.Interfaces;

public interface IAuctionRepository
{
    Task<Subasta?> ObtenerPorIdAsync(int id);
    Task AgregarAsync(Subasta subasta);
    Task<List<Subasta>> ObtenerProgramadasParaIniciarAsync(DateTime fecha, CancellationToken cancellationToken = default);
    Task<List<int>> ObtenerIdsVencidasAsync(DateTime fecha, CancellationToken cancellationToken = default);
}