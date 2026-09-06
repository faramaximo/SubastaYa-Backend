using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Domain.Entities
{
    public class Categoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string ImagenUrl { get; set; } = string.Empty;

        // Navegación
        public ICollection<Subasta> Subastas { get; set; } = new List<Subasta>();
    }
}
