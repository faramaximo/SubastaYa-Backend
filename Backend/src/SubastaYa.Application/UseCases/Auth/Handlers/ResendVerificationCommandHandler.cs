namespace SubastaYa.Application.UseCases.Auth.Handlers;

using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auth.Commands;
using System.Security.Cryptography;
using System.Text;

public class ResendVerificationCommandHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _uow;

    public ResendVerificationCommandHandler(
        IUsuarioRepository usuarios,
        IEmailSender emailSender,
        IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _emailSender = emailSender;
        _uow = uow;
    }

    public async Task Handle(ResendVerificationCommand cmd, string baseUrl, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObtenerPorEmailAsync(cmd.Email);

        // Respuesta genérica para no revelar si el email existe
        if (usuario is null || usuario.EmailVerificado)
            return;

        await EnviarEmailVerificacionAsync(usuario, baseUrl, ct);
    }

    private async Task EnviarEmailVerificacionAsync(
        SubastaYa.Domain.Entities.Usuario usuario, string baseUrl, CancellationToken ct)
    {
        var token = CreateToken();
        usuario.TokenVerificacionHash = HashToken(token);
        usuario.TokenVerificacionExpiraUtc = DateTime.UtcNow.AddHours(24);
        await _uow.SaveChangesAsync(ct);

        var link = $"{baseUrl}/api/auth/verify-email?token={Uri.EscapeDataString(token)}";
        await _emailSender.SendAsync(
            usuario.Email,
            "Verificá tu cuenta",
            $"<p>Confirmá tu correo para activar la cuenta.</p><p><a href=\"{link}\">Verificar correo</a></p><p>Este enlace vence en 24 horas.</p>",
            ct);
    }

    private static string CreateToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
