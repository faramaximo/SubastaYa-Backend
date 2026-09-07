namespace SubastaYa.Application.UseCases.Wallet.Commands
{
    // Es un simple "record" inmutable que transporta los datos desde el Controller hasta el Handler
    public record DepositCommand(int UsuarioId, decimal Monto);
}