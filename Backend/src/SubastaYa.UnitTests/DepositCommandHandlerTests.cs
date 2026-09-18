using NSubstitute;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Wallet.Commands;
using SubastaYa.Application.UseCases.Wallet.Handlers;
using SubastaYa.Domain.Entities;
using Xunit;

namespace SubastaYa.UnitTests;

public class DepositCommandHandlerTests
{
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly ILedgerRepository _ledgerRepository = Substitute.For<ILedgerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    [Fact]
    public async Task Depositar_RegistraTransaccion_Auditoria_Y_ConfirmaUnitOfWork()
    {
        // Arrange
        const int usuarioId = 15;
        const decimal monto = 500m;
        var billetera = new Billetera(usuarioId: usuarioId, saldoTotal: 100m, saldoRetenido: 0m);

        _walletRepository.ObtenerPorUsuarioIdAsync(usuarioId).Returns(billetera);

        var handler = new DepositCommandHandler(_walletRepository, _ledgerRepository, _unitOfWork, _auditService);
        var command = new DepositCommand(usuarioId, monto);

        // Act
        await handler.Handle(command);

        // Assert
        Assert.Equal(600m, billetera.SaldoTotal);

        // 1. Ledger agregado
        await _ledgerRepository.Received(1).AgregarAsync(Arg.Is<TransaccionLedger>(t => t.Monto == monto));

        // 2. Evento de auditoría preparado
        await _auditService.Received(1).RegistrarEventoAsync(
            "Billetera",
            billetera.Id,
            "DEPOSITO_SALDO",
            usuarioId,
            Arg.Any<object>());

        // 3. UnitOfWork confirma la transacción completa
        await _unitOfWork.Received(1).SaveChangesAsync();
    }
}
