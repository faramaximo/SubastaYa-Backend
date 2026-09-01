using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SubastaYa.Domain.Entities
{
    public class Billetera
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public decimal SaldoTotal { get; set; }
        public decimal SaldoRetenido { get; set; }

        // Saldo disponible calculado
        public decimal SaldoDisponible => SaldoTotal - SaldoRetenido;

        // Token de concurrencia optimista (exigido por el TP)
        [Timestamp]
        public byte[] Version { get; set; } = Array.Empty<byte>();

        // Navegación
        public Usuario Usuario { get; set; } = null!;
        public ICollection<TransaccionLedger> Transacciones { get; set; } = new List<TransaccionLedger>();
    }
}
