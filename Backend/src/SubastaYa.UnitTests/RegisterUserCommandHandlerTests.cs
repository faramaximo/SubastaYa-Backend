using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auth.Commands;
using SubastaYa.Application.UseCases.Auth.Handlers;
using SubastaYa.Domain.Entities;
using SubastaYa.Domain.Exceptions;
using Xunit;

namespace SubastaYa.UnitTests;

public class RegisterUserCommandHandlerTests
{
    private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();

    private RegisterUserCommandHandler CreateHandler()
    {
        return new RegisterUserCommandHandler(_usuarioRepository, _unitOfWork, _emailSender);
    }

    [Fact]
    public async Task FlujoExitoso_IniciaTransaccion_Persiste_EnviaEmail_Y_HaceCommit()
    {
        // Arrange
        var command = new RegisterUserCommand("Juan Perez", "juan@example.com", "Password123!", "http://localhost:5000");
        _usuarioRepository.ExisteEmailAsync(command.Email).Returns(false);

        var handler = CreateHandler();

        // Act
        await handler.Handle(command);

        // Assert
        // a. Iniciar transacción
        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());

        // b. Persistir entidad usuario
        await _usuarioRepository.Received(1).AgregarAsync(Arg.Is<Usuario>(u =>
            u.Email == command.Email &&
            u.Nombre == command.Nombre &&
            !u.EmailVerificado &&
            !string.IsNullOrWhiteSpace(u.TokenVerificacionHash) &&
            u.TokenVerificacionExpiraUtc.HasValue));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        // c. Enviar correo de confirmación
        await _emailSender.Received(1).SendAsync(
            command.Email,
            Arg.Any<string>(),
            Arg.Is<string>(body => body.Contains("/api/auth/verify-email?token=")),
            Arg.Any<CancellationToken>());

        // d. Confirmar transacción
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        // No debe haber rollback
        await _unitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FallaEnvioEmail_EjecutaRollback_Y_RelanzaExcepcion()
    {
        // Arrange
        var command = new RegisterUserCommand("Maria Lopez", "maria@example.com", "Password123!", "http://localhost:5000");
        _usuarioRepository.ExisteEmailAsync(command.Email).Returns(false);

        var expectedException = new InvalidOperationException("No se pudo enviar el correo SMTP.");
        _emailSender.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(expectedException);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command));
        Assert.Equal(expectedException.Message, ex.Message);

        // a. Inició transacción
        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());

        // b. Intentó persistir preliminarmente
        await _usuarioRepository.Received(1).AgregarAsync(Arg.Any<Usuario>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        // c. Intentó enviar correo
        await _emailSender.Received(1).SendAsync(command.Email, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        // e. Ejecutó RollbackAsync
        await _unitOfWork.Received(1).RollbackAsync(Arg.Any<CancellationToken>());

        // d. NUNCA ejecutó CommitAsync
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmailYaRegistrado_LanzaDomainException_SinIniciarTransaccion()
    {
        // Arrange
        var command = new RegisterUserCommand("Carlos Gomez", "carlos@example.com", "Password123!");
        _usuarioRepository.ExisteEmailAsync(command.Email).Returns(true);

        var handler = CreateHandler();

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command));

        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _usuarioRepository.DidNotReceive().AgregarAsync(Arg.Any<Usuario>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }
}
