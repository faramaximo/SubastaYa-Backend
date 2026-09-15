using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auth.Commands;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Exceptions;

namespace SubastaYa.Application.UseCases.Auth.Handlers;

public class RegisterUserCommandHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _uow;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration? _configuration;
    private readonly ILogger<RegisterUserCommandHandler>? _logger;

    public RegisterUserCommandHandler(
        IUsuarioRepository usuarios,
        IUnitOfWork uow,
        IEmailSender emailSender,
        IConfiguration? configuration = null,
        ILogger<RegisterUserCommandHandler>? logger = null)
    {
        _usuarios = usuarios;
        _uow = uow;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Handle(RegisterUserCommand cmd, CancellationToken cancellationToken = default)
    {
        var existe = await _usuarios.ExisteEmailAsync(cmd.Email);
        if (existe)
        {
            throw new DomainException("Este correo electrónico ya está registrado.");
        }

        var rawToken = CreateToken();
        var tokenHash = HashToken(rawToken);

        var nuevoUsuario = new Usuario
        {
            Nombre = cmd.Nombre,
            Email = cmd.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(cmd.Password),
            FechaRegistro = DateTime.UtcNow,
            EmailVerificado = false,
            TokenVerificacionHash = tokenHash,
            TokenVerificacionExpiraUtc = DateTime.UtcNow.AddHours(24)
        };

        // a. Iniciar transacción explícita
        await _uow.BeginTransactionAsync(cancellationToken);

        try
        {
            // b. Persistir la entidad Usuario
            await _usuarios.AgregarAsync(nuevoUsuario);
            await _uow.SaveChangesAsync(cancellationToken);

            // c. Confirmar transacción en base de datos
            await _uow.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Si ocurre un error de persistencia, deshacer la transacción
            await _uow.RollbackAsync(cancellationToken);
            throw;
        }

        // d. Intentar el envío de correo de confirmación de forma defensiva
        try
        {
            var baseUrl = !string.IsNullOrWhiteSpace(cmd.BaseUrl)
                ? cmd.BaseUrl
                : (_configuration?["App:BaseUrl"] ?? "http://localhost:5000");

            var verificationLink = $"{baseUrl.TrimEnd('/')}/api/v1/users/email-verifications?token={Uri.EscapeDataString(rawToken)}";
            var subject = "Verificá tu cuenta en SubastaYa";
            var htmlBody = $@"
                <div style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
                    <h2>¡Bienvenido a SubastaYa, {nuevoUsuario.Nombre}!</h2>
                    <p>Por favor confirmá tu dirección de correo electrónico para activar tu cuenta.</p>
                    <p style=""margin: 24px 0;"">
                        <a href=""{verificationLink}"" style=""background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 4px; display: inline-block;"">
                            Verificar correo
                        </a>
                    </p>
                    <p>O copiá y pegá el siguiente enlace en tu navegador:</p>
                    <p><a href=""{verificationLink}"">{verificationLink}</a></p>
                    <p><small>Este enlace vence en 24 horas.</small></p>
                </div>";

            await _emailSender.SendAsync(nuevoUsuario.Email, subject, htmlBody, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Advertencia: No se pudo enviar el correo de verificación a {Email}. El usuario fue registrado con éxito.", nuevoUsuario.Email);
        }
    }

    private static string CreateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
