using SubastaYa.Application.Interfaces;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Enums;

public class DepositCommandHandler
{
    private readonly IWalletRepository _billeteras;
    private readonly ILedgerRepository _ledger; // Repositorio para TransaccionLedger
    private readonly IUnitOfWork _uow;

    public DepositCommandHandler(IWalletRepository billeteras, ILedgerRepository ledger, IUnitOfWork uow)
    {
        _billeteras = billeteras;
        _ledger = ledger;
        _uow = uow;
    }

    public async Task Handle(DepositCommand cmd)
    {
        var billetera = await _billeteras.ObtenerPorUsuarioIdAsync(cmd.UsuarioId);

        if (billetera == null)
        {
            billetera = new Billetera(cmd.UsuarioId); // Usamos el nuevo constructor
            billetera.Depositar(cmd.Monto);
            await _billeteras.AgregarAsync(billetera);
        }
        else
        {
            billetera.Depositar(cmd.Monto); // Usa la lógica rica del dominio
        }

        var transaccion = new TransaccionLedger
        {
            BilleteraId = billetera.Id,
            Monto = cmd.Monto,
            Tipo = TipoTransaccion.Deposito,
            Fecha = DateTime.UtcNow
        };
        await _ledger.AgregarAsync(transaccion);

        // Confirmamos la transacción completa (Atomicidad)[cite: 1]
        await _uow.SaveChangesAsync();
    }
}