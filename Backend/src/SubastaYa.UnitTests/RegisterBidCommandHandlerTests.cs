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
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    private RegisterBidCommandHandler CrearHandler() =>
        new(
            _unitOfWork,
            _auctionRepository,
            _walletRepository,
            _ledgerRepository,
            _notifier,
            _auditService);

    private Subasta CrearSubastaActiva(
        int vendedorId = 99,
        decimal precioBase = 1000m,
        decimal incremento = 100m,
        DateTime? fechaFin = null)
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
            fechaFin: fechaFin ?? DateTime.UtcNow.AddHours(2)
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

        var handler = CrearHandler();
        var command = new RegisterBidCommand(SubastaId: subasta.Id, UsuarioId: 10, Monto: 1000m);

        // Act & Assert
        await Assert.ThrowsAnyAsync<DomainException>(() => handler.Handle(command));

        // Verificamos que NO se guardó nada en la base de datos (atomicidad preservada)
        await _unitOfWork.DidNotReceive().SaveChangesAsync();

        // Verificamos que NO se emitió ninguna notificación SignalR a los clientes
        await _notifier.DidNotReceive().NotificarNuevaPujaAsync(Arg.Any<PujaResponseDto>());
        await _auditService.DidNotReceive().RegistrarEventoAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
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

        var handler = CrearHandler();
        var command = new RegisterBidCommand(SubastaId: subasta.Id, UsuarioId: vendedorId, Monto: 1000m);

        // Act & Assert
        await Assert.ThrowsAnyAsync<DomainException>(() => handler.Handle(command));

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

        var handler = CrearHandler();
        var command = new RegisterBidCommand(SubastaId: subasta.Id, UsuarioId: 2, Monto: 500m);

        // Act & Assert
        await Assert.ThrowsAnyAsync<DomainException>(() => handler.Handle(command));

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

        var handler = CrearHandler();
        var command = new RegisterBidCommand(SubastaId: subasta.Id, UsuarioId: 2, Monto: 1000m);

        // Act
        var resultado = await handler.Handle(command);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(1000m, resultado.Monto);
        Assert.Equal(2, resultado.UsuarioId);
        Assert.False(resultado.TiempoExtendido);
        Assert.NotNull(subasta.UltimaPujaFecha); // Toda puja debe modificar la fila protegida por Version.

        // Verificamos que se confirmó la transacción exactamente 1 vez
        await _unitOfWork.Received(1).SaveChangesAsync();

        // Verificamos que se notificó por SignalR a los clientes
        await _notifier.Received(1).NotificarNuevaPujaAsync(Arg.Is<PujaResponseDto>(p => p.Monto == 1000m && p.UsuarioId == 2));

        // No se debió activar Anti-Sniping porque quedaban 2 horas
        await _auditService.DidNotReceive().RegistrarEventoAsync(
            "Subasta", subasta.Id, "ANTI_SNIPING_ACTIVADO", Arg.Any<int?>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegistrarPuja_ConAntiSniping_RegistraEventoDeAuditoria_Y_ExtiendeTiempo()
    {
        // Arrange: Subasta vence en 30 segundos (<= 60s activa Anti-Sniping)
        var subasta = CrearSubastaActiva(
            vendedorId: 1,
            precioBase: 1000m,
            incremento: 100m,
            fechaFin: DateTime.UtcNow.AddSeconds(30));

        var billetera = new Billetera(usuarioId: 2, saldoTotal: 5000m, saldoRetenido: 0m);

        _auctionRepository.GetByIdWithBidsAsync(subasta.Id).Returns(subasta);
        _walletRepository.GetByUserIdAsync(2).Returns(billetera);
        _unitOfWork.SaveChangesAsync().Returns(1);

        var handler = CrearHandler();
        var command = new RegisterBidCommand(SubastaId: subasta.Id, UsuarioId: 2, Monto: 1000m);

        // Act
        var resultado = await handler.Handle(command);

        // Assert
        Assert.NotNull(resultado);
        Assert.True(resultado.TiempoExtendido);

        // Verificamos registro del evento de auditoría obligatorio
        await _auditService.Received(1).RegistrarEventoAsync(
            "Subasta",
            subasta.Id,
            "ANTI_SNIPING_ACTIVADO",
            command.UsuarioId,
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());

        // Se confirmó el estado en BD
        await _unitOfWork.Received(1).SaveChangesAsync();

        // Se notificó la extensión a los clientes
        await _notifier.Received(1).NotificarNuevaPujaAsync(Arg.Is<PujaResponseDto>(p => p.TiempoExtendido));
    }

    [Fact]
    public async Task Devuelve_conflicto_si_una_oferta_identica_ya_gano_la_carrera()
    {
        // Simula la segunda solicitud: la primera ya registró exactamente la misma oferta.
        var subasta = CrearSubastaActiva(vendedorId: 1, precioBase: 1000m, incremento: 100m);
        subasta.RegistrarPuja(compradorId: 2, monto: 1000m);
        _auctionRepository.GetByIdWithBidsAsync(subasta.Id).Returns(subasta);

        var handler = CrearHandler();

        await Assert.ThrowsAsync<ConcurrencyException>(() =>
            handler.Handle(new RegisterBidCommand(SubastaId: subasta.Id, UsuarioId: 3, Monto: 1000m)));

        await _unitOfWork.DidNotReceive().SaveChangesAsync();
        await _notifier.DidNotReceive().NotificarNuevaPujaAsync(Arg.Any<PujaResponseDto>());
    }}
