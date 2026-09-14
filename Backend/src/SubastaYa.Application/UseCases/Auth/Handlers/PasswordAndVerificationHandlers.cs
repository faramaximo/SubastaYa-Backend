using System.Security.Cryptography;
using System.Text;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auth.Commands;
using SubastaYa.Domain.Exceptions;

namespace SubastaYa.Application.UseCases.Auth.Handlers;

public sealed class ForgotPasswordCommandHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _uow;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordCommandHandler(IUsuarioRepository usuarios, IUnitOfWork uow, IEmailSender emailSender)
    {
        _usuarios = usuarios;
        _uow = uow;
        _emailSender = emailSender;
    }

    public async Task Handle(ForgotPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.ObtenerPorEmailAsync(command.Email);
        if (usuario is null || !usuario.EmailVerificado)
            return;

        var token = TokenSeguro.Crear();
        usuario.TokenRecuperacionHash = TokenSeguro.Hash(token);
        usuario.TokenRecuperacionExpiraUtc = DateTime.UtcNow.AddMinutes(30);
        await _uow.SaveChangesAsync(cancellationToken);

        var link = $"{command.BaseUrl.TrimEnd('/')}/pages/reset-password.html?token={Uri.EscapeDataString(token)}";
        var body = $"<p>Solicitaste restablecer tu contraseña.</p><p><a href=\"{link}\">Restablecer contraseña</a></p><p>Este enlace vence en 30 minutos.</p>";
        await _emailSender.SendAsync(usuario.Email, "Restablecé tu contraseña", body, cancellationToken);
    }
}

public sealed class ResetPasswordCommandHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _uow;

    public ResetPasswordCommandHandler(IUsuarioRepository usuarios, IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _uow = uow;
    }

    public async Task Handle(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.ObtenerPorTokenRecuperacionAsync(TokenSeguro.Hash(command.Token));
        if (usuario is null || usuario.TokenRecuperacionExpiraUtc <= DateTime.UtcNow)
            throw new DomainException("El enlace de recuperación es inválido o expiró.");

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password);
        usuario.TokenRecuperacionHash = null;
        usuario.TokenRecuperacionExpiraUtc = null;
        await _uow.SaveChangesAsync(cancellationToken);
    }
}

public sealed class VerifyEmailCommandHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _uow;

    public VerifyEmailCommandHandler(IUsuarioRepository usuarios, IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _uow = uow;
    }

    public async Task Handle(VerifyEmailCommand command, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.ObtenerPorTokenVerificacionAsync(TokenSeguro.Hash(command.Token));
        if (usuario is null || usuario.TokenVerificacionExpiraUtc <= DateTime.UtcNow)
            throw new DomainException("El enlace de verificación es inválido o expiró.");

        usuario.EmailVerificado = true;
        usuario.TokenVerificacionHash = null;
        usuario.TokenVerificacionExpiraUtc = null;
        await _uow.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ResendEmailVerificationCommandHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _uow;
    private readonly IEmailSender _emailSender;

    public ResendEmailVerificationCommandHandler(IUsuarioRepository usuarios, IUnitOfWork uow, IEmailSender emailSender)
    {
        _usuarios = usuarios;
        _uow = uow;
        _emailSender = emailSender;
    }

    public async Task Handle(ResendEmailVerificationCommand command, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.ObtenerPorEmailAsync(command.Email);
        if (usuario is null || usuario.EmailVerificado)
            return;

        var token = TokenSeguro.Crear();
        usuario.TokenVerificacionHash = TokenSeguro.Hash(token);
        usuario.TokenVerificacionExpiraUtc = DateTime.UtcNow.AddHours(24);
        await _uow.SaveChangesAsync(cancellationToken);

        var link = $"{command.BaseUrl.TrimEnd('/')}/api/v1/users/email-verifications?token={Uri.EscapeDataString(token)}";
        var body = $"<p>Confirmá tu correo para activar tu cuenta.</p><p><a href=\"{link}\">Verificar correo</a></p><p>Este enlace vence en 24 horas.</p>";
        await _emailSender.SendAsync(usuario.Email, "Verificá tu cuenta", body, cancellationToken);
    }
}

internal static class TokenSeguro
{
    public static string Crear() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .Replace("+", "-").Replace("/", "_").TrimEnd('=');

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
