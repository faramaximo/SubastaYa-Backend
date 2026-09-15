using SubastaYa.Domain.Entities;

namespace SubastaYa.Application.Interfaces;

public interface IWalletRepository
{
    Task<Billetera?> ObtenerPorUsuarioIdAsync(int usuarioId);
    Task AgregarAsync(Billetera billetera);
}