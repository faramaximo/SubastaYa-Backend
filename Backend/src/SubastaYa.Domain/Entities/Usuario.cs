using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Domain.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public bool EmailVerificado { get; set; }
        public string? TokenVerificacionHash { get; set; }
        public DateTime? TokenVerificacionExpiraUtc { get; set; }
        public string? TokenRecuperacionHash { get; set; }
        public DateTime? TokenRecuperacionExpiraUtc { get; set; }

        // Navegación
        public Billetera? Billetera { get; set; }
        public ICollection<Subasta> SubastasPublicadas { get; set; } = new List<Subasta>();
        public ICollection<Puja> PujasRealizadas { get; set; } = new List<Puja>();
    }
}
