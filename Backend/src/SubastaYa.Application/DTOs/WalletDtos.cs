using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SubastaYa.Application.DTOs
{
    public record WalletBalanceDto(decimal SaldoTotal, decimal SaldoRetenido, decimal SaldoDisponible);
    public record TransactionDto(int Id, string Tipo, decimal Monto, DateTime Fecha);
}