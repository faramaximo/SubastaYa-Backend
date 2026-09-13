namespace SubastaYa.Application.UseCases.Auth.Handlers;

using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auth.Commands;
using SubastaYa.Domain.Exceptions;
using System.Security.Cryptography;
using System.Text;

public class VerifyEmailCommandHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _uow;

    public VerifyEmailCommandHandler(IUsuarioRepository usuarios, IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _uow = uow;
    }

    public async Task Handle(VerifyEmailCommand cmd)
    {
        var hash = HashToken(cmd.Token);
        var usuario = await _usuarios.ObtenerPorTokenVerificacionAsync(hash);

        if (usuario is null)
            throw new DomainException("El enlace de verificación es inválido o expiró.");

        usuario.EmailVerificado = true;
        usuario.TokenVerificacionHash = null;
        usuario.TokenVerificacionExpiraUtc = null;

        await _uow.SaveChangesAsync();
    }

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
