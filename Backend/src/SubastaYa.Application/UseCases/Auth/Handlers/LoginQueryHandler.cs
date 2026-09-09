namespace SubastaYa.Application.UseCases.Auth.Handlers;
using SubastaYa.Application.UseCases.Auth.Queries;
using SubastaYa.Application.DTOs;
using SubastaYa.Domain.Exceptions;
using SubastaYa.Application.Interfaces;
// Usings mínimos

public class LoginQueryHandler
{
    private readonly IUsuarioRepository _usuarios;

    public LoginQueryHandler(IUsuarioRepository usuarios)
    {
        _usuarios = usuarios;
    }

    public async Task<AuthUserDto> Handle(LoginQuery query)
    {
        var usuario = await _usuarios.ObtenerPorEmailAsync(query.Email);

        if (usuario is null)
        {
            throw new SubastaYa.Domain.Exceptions.UnauthorizedException("Email o contraseña incorrectos.");
        }

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
            throw new SubastaYa.Domain.Exceptions.UnauthorizedException("Email o contraseña incorrectos.");

        return new AuthUserDto(usuario.Id, usuario.Nombre, usuario.Email);
    }
}
