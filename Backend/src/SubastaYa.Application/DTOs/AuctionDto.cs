using System;
using SubastaYa.Domain.Enums;

namespace SubastaYa.Application.DTOs;

public class AuctionDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string UrlImagen { get; set; } = string.Empty;
    public string CategoriaNombre { get; set; } = string.Empty;

    public decimal PrecioBase { get; set; }
    public decimal IncrementoMinimo { get; set; }

    // Oferta más alta actual (si aplica)
    public decimal OfertaMasAlta { get; set; }
    public int CantidadOfertas { get; set; }

    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public EstadoSubasta Estado { get; set; }

    // Lista de pujas simplificadas para evitar ciclos de serialización
    public List<PujaInfoDto> Pujas { get; set; } = new List<PujaInfoDto>();
}
