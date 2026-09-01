using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Domain.Entities
{
    public class AuditoriaLog
    {
        public int Id { get; set; }
        public string Entidad { get; set; } = string.Empty;
        public int EntidadId { get; set; }
        public string Accion { get; set; } = string.Empty;
        public int? UsuarioId { get; set; }
        public string DetalleJson { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
