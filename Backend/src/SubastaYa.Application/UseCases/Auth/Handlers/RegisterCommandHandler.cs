namespace SubastaYa.Application.UseCases.Auth.Handlers;
using SubastaYa.Application.UseCases.Auth.Commands;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Exceptions;
using SubastaYa.Application.Interfaces;

public class RegisterCommandHandler
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUnitOfWork _uow;

    public RegisterCommandHandler(IUsuarioRepository usuarios, IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _uow = uow;
    }

    public async Task Handle(RegisterCommand cmd)
    {
        var existe = await _usuarios.ExisteEmailAsync(cmd.Email);
        if (existe)
        {
            // El middleware atrapará esto y devolverá 400. 
            throw new DomainException("Este correo electrónico ya está registrado.");
        }

        var nuevoUsuario = new Usuario
        {
            Nombre = cmd.Nombre,
            Email = cmd.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(cmd.Password),
            FechaRegistro = DateTime.UtcNow,
            EmailVerificado = false
        };

        // Regla de negocio: Billetera en cero. 
        // Si Usuario tiene una propiedad de navegación hacia Billetera, EF Core enlaza los IDs automáticamente al guardar.
        // nuevoUsuario.Billetera = new Billetera { SaldoTotal = 0, SaldoRetenido = 0 };
        // Si no la tenés mapeada así, dejás el _billeteraRepository.Agregar(nuevaBilletera) que tenías.

        await _usuarios.AgregarAsync(nuevoUsuario);

        // El repositorio prepara, el caso de uso decide cuándo confirmar la transacción.
        await _uow.SaveChangesAsync();
    }
}
