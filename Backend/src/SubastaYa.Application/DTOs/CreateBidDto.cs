using System.ComponentModel.DataAnnotations;

namespace SubastaYa.Application.DTOs;

public class CreateBidDto
{
    public int UsuarioId { get; set; } // Opcional, idealmente de Auth.

    [Required(ErrorMessage = "El monto de la puja es obligatorio.")]
    [Range(typeof(decimal), "0.01", "10000000", ErrorMessage = "El monto debe ser mayor a cero y menor a $10.000.000.")]
    public decimal Monto { get; set; }
}
