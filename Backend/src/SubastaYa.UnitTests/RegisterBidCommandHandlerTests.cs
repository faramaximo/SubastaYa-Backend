using NSubstitute;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Bids.Commands;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Exceptions;
using Xunit;

namespace SubastaYa.UnitTests;

public class RegisterBidCommandHandlerTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuctionRepository _auctionRepository = Substitute.For<IAuctionRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly ILedgerRepository _ledgerRepository = Substitute.For<ILedgerRepository>();
    private readonly IAuctionNotifier _notifier = Substitute.For<IAuctionNotifier>();

    private Subasta CrearSubastaActiva(int vendedorId = 99, decimal precioBase = 1000m, decimal incremento = 100m)
    {
        return new Subasta(
            vendedorId: vendedorId,
            categoriaId: 1,
            titulo: "Notebook Gamer",
            descripcion: "Excelente estado",
            urlImagen: "https://example.com/img.jpg",
            precioBase: precioBase,
            incrementoMinimo: incremento,
            fechaInicio: DateTime.UtcNow.AddHours(-1),
            fechaFin: DateTime.UtcNow.AddHours(2)
        );
    }

    [Fact]
    public async Task No_persiste_ni_notifica_si_saldo_en_billetera_es_insuficiente()
    {
        // Arrange (Mocks: sin base de datos, sin SignalR levantado)
        var subasta = CrearSubastaActiva(vendedorId: 99, precioBase: 1000m);
        var billetera = new Billetera(usuarioId: 10, saldoTotal: 200m, saldoRetenido: 0m); // Solo $200 disponibles

        _auctionRepository.GetByIdWithBidsAsync(subasta.Id).Returns(subasta);
        _walletRepository.GetByUserIdAsync(10).Returns(billetera);

        var handler = new RegisterBidCommandHandler(
            _unitOfWork,
            _auctionRepository,
            _walletRepository,
            _ledgerRepository,
            _notifier);

        var command = new RegisterBidCommand(SubastaId: subasta.Id, UsuarioId: 10, Monto: 1000m);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command));

        // Verificamos que NO se guardó nada en la base de datos (atomicidad preservada)
        await _unitOfWork.DidNotReceive().SaveChangesAsync();

        // Verificamos que NO se emitió ninguna notificación SignalR a los clientes
        await _notifier.DidNotReceive().NotificarNuevaPujaAsync(Arg.Any<PujaResponseDto>());
    }

    [Fact]
    public async Task No_persiste_ni_notifica_si_el_vendedor_intenta_pujar_en_su_propia_subasta()
    {
        // Arrange
        const int vendedorId = 42;
        var subasta = CrearSubastaActiva(vendedorId: vendedorId, precioBase: 1000m);
        var billetera = new Billetera(usuarioId: vendedorId, saldoTotal: 5000m, saldoRetenido: 0m);

        _auctionRepository.GetByIdWithBidsAsync(subasta.Id).Returns(subasta);
        _walletRepository.GetByUserIdAsync(vendedorId).Returns(billetera);

        var handler = new RegisterBidCommandHandler(
            _unitOfWork,
            _auctionRepository,
            _walletRepository,
            _ledgerRepository,
            _notifier);

        // El usuario 42 es el dueño de la subasta
        var command = new RegisterBidCommand(SubastaId: subasta.Id, UsuarioId: vendedorId, Monto: 1000m);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command));

        await _unitOfWork.DidNotReceive().SaveChangesAsync();
        await _notifier.DidNotReceive().NotificarNuevaPujaAsync(Arg.Any<PujaResponseDto>());
    }

    [Fact]
    public async Task No_persiste_si_el_monto_ofertado_es_menor_a_la_puja_minima()
    {
        // Arrange
        var subasta = CrearSubastaActiva(vendedorId: 1, precioBase: 1000m, incremento: 100m);
        var billetera = new Billetera(usuarioId: 2, saldoTotal: 5000m, saldoRetenido: 0m);

        _auctionRepository.GetByIdWithBidsAsync(subasta.Id).Returns(subasta);
        _walletRepository.GetByUserIdAsync(2).Returns(billetera);

        var handler = new RegisterBidCommandHandler(
            _unitOfWork,
            _auctionRepository,
            _walletRepository,
            _ledgerRepository,
            _notifier);

        // Ofrecemos $500 cuando el mínimo requerido es $1000
        var command = new RegisterBidCommand(SubastaId: subasta.Id, UsuarioId: 2, Monto: 500m);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command));

        await _unitOfWork.DidNotReceive().SaveChangesAsync();
        await _notifier.DidNotReceive().NotificarNuevaPujaAsync(Arg.Any<PujaResponseDto>());
    }

    [Fact]
    public async Task Persiste_y_notifica_por_signalr_cuando_la_puja_es_valida()
    {
        // Arrange
        var subasta = CrearSubastaActiva(vendedorId: 1, precioBase: 1000m, incremento: 100m);
        var billetera = new Billetera(usuarioId: 2, saldoTotal: 5000m, saldoRetenido: 0m);

        _auctionRepository.GetByIdWithBidsAsync(subasta.Id).Returns(subasta);
        _walletRepository.GetByUserIdAsync(2).Returns(billetera);
        _unitOfWork.SaveChangesAsync().Returns(1);

        var handler = new RegisterBidCommandHandler(
            _unitOfWork,
            _auctionRepository,
            _walletRepository,
            _ledgerRepository,
            _notifier);

        var command = new RegisterBidCommand(SubastaId: subasta.Id, UsuarioId: 2, Monto: 1000m);

        // Act
        var resultado = await handler.Handle(command);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(1000m, resultado.Monto);
        Assert.Equal(2, resultado.UsuarioId);

        // Verificamos que se confirmó la transacción exactamente 1 vez
        await _unitOfWork.Received(1).SaveChangesAsync();

        // Verificamos que se notificó por SignalR a los clientes
        await _notifier.Received(1).NotificarNuevaPujaAsync(Arg.Is<PujaResponseDto>(p => p.Monto == 1000m && p.UsuarioId == 2));
    }
}
