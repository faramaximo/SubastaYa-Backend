using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Entities;
using SubastaYa.Infrastructure.Data;

namespace SubastaYa.Infrastructure.Persistence.Repositories;

public class AuditoriaRepository : IAuditoriaRepository
{
    private readonly SubastaYaDbContext _context;

    public AuditoriaRepository(SubastaYaDbContext context)
    {
        _context = context;
    }

    public async Task AgregarAsync(AuditoriaLog log, CancellationToken cancellationToken = default)
    {
        await _context.AuditoriasLog.AddAsync(log, cancellationToken);
    }
}
