using System;

namespace SubastaYa.Application.DTOs;

public class CreateAuctionDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string UrlImagen { get; set; } = string.Empty;
    public int CategoriaId { get; set; }

    // El VendedorId en un caso real se tomara de Auth/Claims, por ahora lo pasamos.
    public int VendedorId { get; set; }

    public decimal PrecioBase { get; set; }
    public decimal IncrementoMinimo { get; set; }

    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}
