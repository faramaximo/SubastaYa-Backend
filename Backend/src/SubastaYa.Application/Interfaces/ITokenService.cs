namespace SubastaYa.Application.Interfaces;

public interface ITokenService
{
    string GenerarTokenAcceso(int usuarioId, string nombre, string email);
}
