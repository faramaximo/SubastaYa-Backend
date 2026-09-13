namespace SubastaYa.Application.UseCases.Auth.Handlers;

using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auth.Commands;
using System.Security.Cryptography;
using System.Text;

public class ForgotPasswordCommandHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IEmailSender _emailSender;
    private readonly IUnitOfWork _uow;

    public ForgotPasswordCommandHandler(
        IUsuarioRepository usuarios,
        IEmailSender emailSender,
        IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _emailSender = emailSender;
        _uow = uow;
    }

    public async Task Handle(ForgotPasswordCommand cmd, CancellationToken ct = default)
    {
        var usuario = await _usuarios.ObtenerPorEmailAsync(cmd.Email);

        // Respuesta genérica para no revelar si el email existe
        if (usuario is null || !usuario.EmailVerificado)
            return;

        var token = CreateToken();
        usuario.TokenRecuperacionHash = HashToken(token);
        usuario.TokenRecuperacionExpiraUtc = DateTime.UtcNow.AddMinutes(30);
        await _uow.SaveChangesAsync(ct);

        var link = $"{cmd.BaseUrl}/pages/reset-password.html?token={Uri.EscapeDataString(token)}";
        await _emailSender.SendAsync(
            usuario.Email,
            "Restablecé tu contraseña",
            $"<p>Solicitaste restablecer tu contraseña.</p><p><a href=\"{link}\">Restablecer contraseña</a></p><p>Este enlace vence en 30 minutos.</p>",
            ct);
    }

    private static string CreateToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
