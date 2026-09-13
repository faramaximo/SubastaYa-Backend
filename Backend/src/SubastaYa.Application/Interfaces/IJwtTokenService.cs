namespace SubastaYa.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerarToken(int userId, string nombre, string email);
}
