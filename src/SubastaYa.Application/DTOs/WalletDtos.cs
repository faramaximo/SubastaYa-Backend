using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace SubastaYa.Application.DTOs
{
    public class WalletBalanceDto
    {
        public int BilleteraId { get; set; }
        public int UsuarioId { get; set; }
        public decimal SaldoTotal { get; set; }
        public decimal SaldoRetenido { get; set; }
        public decimal SaldoDisponible { get; set; }
    }

    public class DepositRequestDto
    {
        [JsonPropertyName("monto")]
        public decimal Monto { get; set; }
    }

    public class TransactionLedgerDto
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public int? SubastaId { get; set; }
    }
}
