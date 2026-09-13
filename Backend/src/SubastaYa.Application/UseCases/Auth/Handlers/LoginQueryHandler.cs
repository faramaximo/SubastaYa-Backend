namespace SubastaYa.Application.UseCases.Auth.Handlers;
using SubastaYa.Application.UseCases.Auth.Queries;
using SubastaYa.Application.DTOs;
using SubastaYa.Domain.Exceptions;
using SubastaYa.Application.Interfaces;

public class LoginQueryHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IJwtTokenService _jwt;

    public LoginQueryHandler(IUsuarioRepository usuarios, IJwtTokenService jwt)
    {
        _usuarios = usuarios;
        _jwt = jwt;
    }

    public async Task<LoginResponseDto> Handle(LoginQuery query)
    {
        var usuario = await _usuarios.ObtenerPorEmailAsync(query.Email);

        if (usuario is null)
            throw new UnauthorizedException("Email o contraseña incorrectos.");

        bool passwordValida;
        try
        {
            passwordValida = BCrypt.Net.BCrypt.Verify(query.Password, usuario.PasswordHash);
        }
        catch (Exception)
        {
            // Una contraseña heredada sin hash BCrypt no debe convertirse en HTTP 500.
            passwordValida = false;
        }

        if (!passwordValida)
            throw new UnauthorizedException("Email o contraseña incorrectos.");

        if (!usuario.EmailVerificado)
            throw new UnauthorizedException("Verificá tu correo electrónico antes de ingresar.");

        var token = _jwt.GenerarToken(usuario.Id, usuario.Nombre, usuario.Email);
        return new LoginResponseDto(usuario.Id, usuario.Nombre, usuario.Email, token);
    }
}
