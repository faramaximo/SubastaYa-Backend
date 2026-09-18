namespace SubastaYa.Application.UseCases.Auth.Commands;

public record RegisterUserCommand(
    string Nombre,
    string Email,
    string Password,
    string? BaseUrl = null
);

