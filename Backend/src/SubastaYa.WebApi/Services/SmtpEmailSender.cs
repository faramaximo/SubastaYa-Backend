using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using SubastaYa.Application.Interfaces;

namespace SubastaYa.WebApi.Services;

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var host = configuration["Email:SmtpHost"];
        var sender = configuration["Email:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(sender))
            throw new InvalidOperationException("El servicio de correo no está configurado.");

        var port = configuration.GetValue<int?>("Email:Port");
        var useSsl = configuration.GetValue<bool?>("Email:UseSsl");
        var username = configuration["Email:Username"];
        var rawPassword = configuration["Email:Password"];

        if (port is not (> 0 and <= 65535) || useSsl is null || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(rawPassword))
             throw new InvalidOperationException("La configuración SMTP de la sección Email está incompleta o es inválida.");

        using var message = new MailMessage(sender, recipient, subject, htmlBody) { IsBodyHtml = true };
        using var client = new SmtpClient(host, port.Value);

        // 1. PRIMERO deshacer las credenciales por defecto (Si esto va abajo, borra la propiedad Credentials)
        client.UseDefaultCredentials = false;

        // 2. Asignar SSL/TLS y forma de entrega
        client.EnableSsl = useSsl.Value;
        client.DeliveryMethod = SmtpDeliveryMethod.Network;
        client.Timeout = 10000;

        // 3. ÚLTIMO asignar credenciales leídas desde configuración segura.
        client.Credentials = new NetworkCredential(username, rawPassword);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (SmtpException ex)
        {
            throw new InvalidOperationException("No se pudo enviar el correo. Revisá la configuración SMTP.", ex);
        }
    }
}
