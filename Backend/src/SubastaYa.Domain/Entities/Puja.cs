using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Domain.Entities
{
    public class Puja
    {
        public int Id { get; set; }
        public int SubastaId { get; set; }
        public int CompradorId { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaPuja { get; set; } = DateTime.UtcNow;

        // Navegación
        public Subasta Subasta { get; set; } = null!;
        public Usuario Comprador { get; set; } = null!;
    }
}
