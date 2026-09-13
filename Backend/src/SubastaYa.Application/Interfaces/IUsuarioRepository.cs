// SubastaYa.Application/Interfaces/IUsuarioRepository.cs
using SubastaYa.Domain.Entities;

namespace SubastaYa.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorEmailAsync(string email);
    Task<bool> ExisteEmailAsync(string email);
    Task AgregarAsync(Usuario usuario);
    Task<Usuario?> ObtenerPorTokenVerificacionAsync(string tokenHash);
    Task<Usuario?> ObtenerPorTokenRecuperacionAsync(string tokenHash);
    // Recordá la regla: NO hay SaveChangesAsync acá.
}