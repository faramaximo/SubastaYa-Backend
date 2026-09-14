using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Domain.Entities
{
    public class AuditoriaLog
    {
        public int Id { get; init; }
        public string Entidad { get; init; } = string.Empty;
        public int EntidadId { get; init; }
        public string Accion { get; init; } = string.Empty;
        public int? UsuarioId { get; init; }
        public string DetalleJson { get; init; } = string.Empty;
        public DateTime Fecha { get; init; } = DateTime.UtcNow;   
        public Usuario? Usuario { get; init; }
    }
}
