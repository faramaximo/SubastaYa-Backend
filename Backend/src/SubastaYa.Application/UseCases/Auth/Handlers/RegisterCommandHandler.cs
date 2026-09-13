namespace SubastaYa.Application.UseCases.Auth.Handlers;

using Microsoft.Extensions.Configuration;
using SubastaYa.Application.Interfaces;

/// <summary>
/// Mantiene compatibilidad con implementaciones anteriores delegando en RegisterUserCommandHandler.
/// </summary>
public class RegisterCommandHandler : RegisterUserCommandHandler
{
    public RegisterCommandHandler(
        IUsuarioRepository usuarios,
        IUnitOfWork uow,
        IEmailSender emailSender,
        IConfiguration? configuration = null)
        : base(usuarios, uow, emailSender, configuration)
    {
    }
}
