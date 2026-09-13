using SubastaYa.Domain.Entities;

namespace SubastaYa.Application.Interfaces;

public interface IAuditoriaRepository
{
    Task AgregarAsync(AuditoriaLog log, CancellationToken cancellationToken = default);
}
