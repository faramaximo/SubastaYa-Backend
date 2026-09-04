using SubastaYa.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Application.Interfaces
{
    public interface IWalletService
    {
        Task<WalletBalanceDto?> GetBalanceByUserIdAsync(int usuarioId);
        Task<bool> DepositAsync(int usuarioId, decimal monto);
        Task<IEnumerable<TransactionLedgerDto>> GetTransactionsByUserIdAsync(int usuarioId);
    }
}
