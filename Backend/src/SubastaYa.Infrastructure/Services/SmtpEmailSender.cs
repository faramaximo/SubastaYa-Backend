using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubastaYa.Application.Interfaces;

namespace SubastaYa.Infrastructure.Services;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender>? _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Email:SmtpHost"];
        var sender = _configuration["Email:From"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(sender))
        {
            const string configError = "El servicio de correo no está configurado: 'Email:SmtpHost' o 'Email:From' no fueron especificados.";
            _logger?.LogError(configError);
            throw new InvalidOperationException(configError);
        }

        var port = _configuration.GetValue<int?>("Email:Port") ?? 2525;
        var useSsl = _configuration.GetValue<bool?>("Email:UseSsl") ?? false;
        var username = _configuration["Email:Username"];
        var rawPassword = _configuration["Email:Password"];

        if (port is not (> 0 and <= 65535))
        {
            const string portError = "El puerto SMTP configurado es inválido: debe estar entre 1 y 65535.";
            _logger?.LogError(portError);
            throw new InvalidOperationException(portError);
        }

        using var message = new MailMessage(sender, recipient, subject, htmlBody)
        {
            IsBodyHtml = true
        };

        using var client = new SmtpClient(host, port);

        // 1. Método de entrega en red
        client.DeliveryMethod = SmtpDeliveryMethod.Network;

        // 2. Deshabilitar credenciales por defecto
        client.UseDefaultCredentials = false;

        // 3. Asignar credenciales de autenticación si fueron proporcionadas (ej. opcional en Mailpit)
        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, rawPassword ?? string.Empty);
        }

        // 4. Configurar SSL / STARTTLS
        client.EnableSsl = useSsl;
        client.Timeout = 15000;

        try
        {
            _logger?.LogInformation("Iniciando despacho de correo a {Recipient} mediante {Host}:{Port} (EnableSsl: {EnableSsl})...",
                recipient, host, port, client.EnableSsl);

            await client.SendMailAsync(message, cancellationToken);

            _logger?.LogInformation("Correo enviado exitosamente a {Recipient}.", recipient);
        }
        catch (SmtpException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? "Sin detalles internos";
            var detailedError = $"Fallo al enviar correo SMTP a '{recipient}'. StatusCode: {ex.StatusCode} ({(int)ex.StatusCode}). Mensaje: {ex.Message}. Detalle interno: {innerMessage}";

            _logger?.LogError(ex, "Error SMTP [{StatusCode}] al enviar correo a {Recipient}: {ErrorMessage}. Detalle interno: {InnerMessage}",
                ex.StatusCode, recipient, ex.Message, innerMessage);

            throw new InvalidOperationException(detailedError, ex);
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException?.Message ?? "Sin detalles internos";
            var detailedError = $"Error general inesperado al despachar correo a '{recipient}'. Mensaje: {ex.Message}. Detalle interno: {innerMessage}";

            _logger?.LogError(ex, "Error general al enviar correo a {Recipient}: {ErrorMessage}. Detalle interno: {InnerMessage}",
                recipient, ex.Message, innerMessage);

            throw new InvalidOperationException(detailedError, ex);
        }
    }
}
