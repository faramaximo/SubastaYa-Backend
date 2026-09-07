using SubastaYa.Domain.Enums;

namespace SubastaYa.Application.DTOs
{
    public record PublicacionResumenDto(
        int Id,
        string Titulo,
        string UrlImagen,
        EstadoSubasta Estado,
        DateTime FechaFin,
        decimal PrecioBase,
        decimal OfertaMasAlta,
        int CantidadPujas
    );

    public record ParticipacionResumenDto(
        int Id,
        string Titulo,
        string UrlImagen,
        EstadoSubasta Estado,
        DateTime FechaFin,
        decimal MiMaximaPuja,
        decimal OfertaGanadora
    );
}