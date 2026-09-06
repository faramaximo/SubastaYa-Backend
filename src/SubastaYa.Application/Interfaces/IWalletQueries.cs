using SubastaYa.Application.DTOs;

namespace SubastaYa.Application.Interfaces
{
    public interface IWalletQueries
    {
        Task<WalletBalanceDto?> GetBalanceAsync(int usuarioId);
        Task<List<TransactionDto>> GetTransactionsAsync(int usuarioId);
    }
}