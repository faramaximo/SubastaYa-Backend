namespace SubastaYa.Application.UseCases.Auth.Handlers;
using SubastaYa.Application.UseCases.Auth.Queries;
using SubastaYa.Application.DTOs;
using SubastaYa.Domain.Exceptions;
using SubastaYa.Application.Interfaces;

public class LoginQueryHandler
{
    private readonly IUsuarioRepository _usuarios;

    public LoginQueryHandler(IUsuarioRepository usuarios)
    {
        _usuarios = usuarios;
    }

    public async Task<object> Handle(LoginQuery query)
    {
        var usuario = await _usuarios.ObtenerPorEmailAsync(query.Email);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(query.Password, usuario.PasswordHash))
        {
            // Lanzamos la excepción de negocio; el middleware la convertirá en un error HTTP.
            throw new DomainException("Email o contraseña incorrectos.");
        }

        return new
        {
            id = usuario.Id,
            nombre = usuario.Nombre,
            email = usuario.Email
        };
    }
}