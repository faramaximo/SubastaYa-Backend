public interface IWalletRepository
{
    Task<Billetera?> ObtenerPorUsuarioIdAsync(int usuarioId);
    // Método requerido por BidService: obtener la billetera por el id de usuario
    Task<Billetera?> GetByUserIdAsync(int usuarioId);
    Task AgregarAsync(Billetera billetera);
}