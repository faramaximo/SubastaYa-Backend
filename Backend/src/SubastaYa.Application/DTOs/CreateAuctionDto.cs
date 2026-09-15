using System;
using System.ComponentModel.DataAnnotations;

namespace SubastaYa.Application.DTOs;

public class CreateAuctionDto
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "El título debe tener entre 3 y 200 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "La descripción debe tener entre 10 y 2000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La URL de imagen es obligatoria.")]
    [Url(ErrorMessage = "La URL de imagen no tiene un formato válido.")]
    public string UrlImagen { get; set; } = string.Empty;

    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una categoría válida.")]
    public int CategoriaId { get; set; }

    // El VendedorId en un caso real se tomará de Auth/Claims, por ahora lo pasamos.
    public int VendedorId { get; set; }

    [Required(ErrorMessage = "El precio base es obligatorio.")]
    [Range(typeof(decimal), "0.01", "10000000", ErrorMessage = "El precio base debe ser mayor a cero y menor a $10.000.000.")]
    public decimal PrecioBase { get; set; }

    [Required(ErrorMessage = "El incremento mínimo es obligatorio.")]
    [Range(typeof(decimal), "0.01", "10000000", ErrorMessage = "El incremento mínimo debe ser mayor a cero.")]
    public decimal IncrementoMinimo { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    public DateTime FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
    public DateTime FechaFin { get; set; }
}
