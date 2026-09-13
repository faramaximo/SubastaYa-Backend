using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Wallet.Commands;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Enums;
using System;
using System.Threading.Tasks;

public class DepositCommandHandler
{
    private readonly IWalletRepository _billeteras;
    private readonly ILedgerRepository _ledger;
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;

    public DepositCommandHandler(
        IWalletRepository billeteras,
        ILedgerRepository ledger,
        IUnitOfWork uow,
        IAuditService auditService)
    {
        _billeteras = billeteras;
        _ledger = ledger;
        _uow = uow;
        _auditService = auditService;
    }

    public async Task Handle(DepositCommand cmd)
    {
        var billetera = await _billeteras.ObtenerPorUsuarioIdAsync(cmd.UsuarioId);

        if (billetera == null)
        {
            billetera = new Billetera(cmd.UsuarioId);
            billetera.Depositar(cmd.Monto);
            await _billeteras.AgregarAsync(billetera);
        }
        else
        {
            billetera.Depositar(cmd.Monto);
        }

        var transaccion = new TransaccionLedger
        {
            Billetera = billetera,
            Monto = cmd.Monto,
            Tipo = TipoTransaccion.Deposito,
            Fecha = DateTime.UtcNow
        };
        await _ledger.AgregarAsync(transaccion);

        // Registro de auditoría (preparación en el contexto de trabajo)
        await _auditService.RegistrarEventoAsync(
            entidad: "Billetera",
            entidadId: billetera.Id,
            accion: "DEPOSITO_SALDO",
            usuarioId: cmd.UsuarioId,
            detalle: new
            {
                billeteraId = billetera.Id,
                usuarioId = cmd.UsuarioId,
                monto = cmd.Monto,
                saldoTotal = billetera.SaldoTotal,
                saldoDisponible = billetera.SaldoDisponible,
                tipo = TipoTransaccion.Deposito.ToString(),
                fecha = DateTime.UtcNow
            }
        );

        // Confirmamos la transacción completa (Atomicidad)
        await _uow.SaveChangesAsync();
    }
}