using System;
using System.Collections.Generic;
using System.Text;

namespace SubastaYa.Application.DTOs
{
    public record PujaResponseDto(
        int Id,
        int SubastaId,
        int UsuarioId,
        decimal Monto,
        DateTime FechaHora,
        DateTime NuevaFechaFin,
        bool TiempoExtendido
    );
}
