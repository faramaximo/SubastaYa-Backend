using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
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

    public RegisterUserCommandHandler(
        IUsuarioRepository usuarios,
        IUnitOfWork uow,
        IEmailSender emailSender,
        IConfiguration? configuration = null)
    {
        _usuarios = usuarios;
        _uow = uow;
        _emailSender = emailSender;
        _configuration = configuration;
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
            // b. Persistir preliminarmente la entidad Usuario
            await _usuarios.AgregarAsync(nuevoUsuario);
            await _uow.SaveChangesAsync(cancellationToken);

            // c. Intentar el envío del correo de confirmación mediante IEmailSender
            var baseUrl = !string.IsNullOrWhiteSpace(cmd.BaseUrl)
                ? cmd.BaseUrl
                : (_configuration?["App:BaseUrl"] ?? "http://localhost:5000");

            var verificationLink = $"{baseUrl.TrimEnd('/')}/api/auth/verify-email?token={Uri.EscapeDataString(rawToken)}";
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

            // d. Si el correo se envía correctamente, ejecutar CommitAsync() de la transacción
            await _uow.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            // e. Si ocurre una excepción (tanto en BD como en el envío SMTP), ejecutar RollbackAsync()
            // de la transacción para que el usuario no quede creado en estado inconsistente,
            // y relanzar la excepción para ser capturada por el ExceptionMiddleware.
            await _uow.RollbackAsync(cancellationToken);
            throw;
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
