namespace SubastaYa.Application.DTOs;

public class CreateBidDto
{
    public int UsuarioId { get; set; } // Opcional, idealmente de Auth.
    public decimal Monto { get; set; }
}
