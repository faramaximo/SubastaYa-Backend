using System;

namespace SubastaYa.Application.DTOs
{
    public record PujaInfoDto(
        int Id,
        int CompradorId,
        decimal Monto,
        DateTime FechaPuja
    );
}
