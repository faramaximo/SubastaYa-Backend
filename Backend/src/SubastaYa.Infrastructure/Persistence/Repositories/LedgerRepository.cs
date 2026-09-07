using System.Threading.Tasks;
using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Entities;
using SubastaYa.Infrastructure.Data;

namespace SubastaYa.Infrastructure.Persistence.Repositories
{
    public class LedgerRepository : ILedgerRepository
    {
        private readonly SubastaYaDbContext _context;

        public LedgerRepository(SubastaYaDbContext context)
        {
            _context = context;
        }

        public async Task AgregarAsync(TransaccionLedger transaccion)
        {
            await _context.TransaccionesLedger.AddAsync(transaccion);
        }
    }
}