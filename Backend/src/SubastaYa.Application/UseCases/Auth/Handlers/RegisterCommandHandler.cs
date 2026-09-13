namespace SubastaYa.Application.UseCases.Auth.Handlers;
using SubastaYa.Application.UseCases.Auth.Commands;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Exceptions;
using SubastaYa.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

public class RegisterCommandHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _uow;
    private readonly IEmailSender _emailSender;

    public RegisterCommandHandler(IUsuarioRepository usuarios, IUnitOfWork uow, IEmailSender emailSender)
    {
        _usuarios = usuarios;
        _uow = uow;
        _emailSender = emailSender;
    }

    public async Task Handle(RegisterCommand cmd, string baseUrl, CancellationToken ct = default)
    {
        var existe = await _usuarios.ExisteEmailAsync(cmd.Email);
        if (existe)
            throw new DomainException("Este correo electrónico ya está registrado.");

        var tokenPlano = CreateToken();

        var nuevoUsuario = new Usuario
        {
            Nombre = cmd.Nombre,
            Email = cmd.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(cmd.Password),
            FechaRegistro = DateTime.UtcNow,
            EmailVerificado = false,
            TokenVerificacionHash = HashToken(tokenPlano),
            TokenVerificacionExpiraUtc = DateTime.UtcNow.AddHours(24)
        };

        await _usuarios.AgregarAsync(nuevoUsuario);
        await _uow.SaveChangesAsync(ct);

        // Enviar email de verificación
        var link = $"{baseUrl}/api/auth/verify-email?token={Uri.EscapeDataString(tokenPlano)}";
        await _emailSender.SendAsync(
            nuevoUsuario.Email,
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
