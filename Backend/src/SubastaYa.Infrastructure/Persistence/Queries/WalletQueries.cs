using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using SubastaYa.Infrastructure.Data;

namespace SubastaYa.Infrastructure.Persistence.Queries
{
    public class WalletQueries : IWalletQueries
    {
        private readonly SubastaYaDbContext _ctx;

        public WalletQueries(SubastaYaDbContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<WalletBalanceDto?> GetBalanceAsync(int usuarioId)
        {
            return await _ctx.Billeteras
                .AsNoTracking()
                .Where(b => b.UsuarioId == usuarioId)
                .Select(b => new WalletBalanceDto(b.SaldoTotal, b.SaldoRetenido, b.SaldoDisponible))
                .FirstOrDefaultAsync();
        }

        public async Task<List<TransactionDto>> GetTransactionsAsync(int usuarioId)
        {
            return await _ctx.TransaccionesLedger
                .AsNoTracking()
                .Where(t => t.Billetera.UsuarioId == usuarioId)
                .OrderByDescending(t => t.Fecha)
                .Select(t => new TransactionDto(t.Id, t.Tipo.ToString(), t.Monto, t.Fecha))
                .ToListAsync();
        }
    }
}