using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Application.UseCases.Wallet.Queries
{
    public record GetBalanceQuery(int UsuarioId);
    public record GetTransactionsQuery(int UsuarioId);
}