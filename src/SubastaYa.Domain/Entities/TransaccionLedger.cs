using SubastaYa.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Domain.Entities
{
    public class TransaccionLedger
    {
        public int Id { get; set; }
        public int BilleteraId { get; set; }
        public TipoTransaccion Tipo { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public int? SubastaId { get; set; }

        // Navegación
        public Billetera Billetera { get; set; } = null!;
        public Subasta? Subasta { get; set; }
    }
}
