namespace SubastaYa.Application.UseCases.Auth.Handlers;

using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auth.Commands;
using SubastaYa.Domain.Exceptions;
using System.Security.Cryptography;
using System.Text;

public class ResetPasswordCommandHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _uow;

    public ResetPasswordCommandHandler(IUsuarioRepository usuarios, IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _uow = uow;
    }

    public async Task Handle(ResetPasswordCommand cmd)
    {
        var hash = HashToken(cmd.Token);
        var usuario = await _usuarios.ObtenerPorTokenRecuperacionAsync(hash);

        if (usuario is null)
            throw new DomainException("El enlace de recuperación es inválido o expiró.");

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(cmd.Password);
        usuario.TokenRecuperacionHash = null;
        usuario.TokenRecuperacionExpiraUtc = null;

        await _uow.SaveChangesAsync();
    }

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
