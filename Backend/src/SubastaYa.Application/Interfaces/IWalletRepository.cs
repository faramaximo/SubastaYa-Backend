public interface IWalletRepository
{
    Task<Billetera?> ObtenerPorUsuarioIdAsync(int usuarioId);
    Task AgregarAsync(Billetera billetera);
}