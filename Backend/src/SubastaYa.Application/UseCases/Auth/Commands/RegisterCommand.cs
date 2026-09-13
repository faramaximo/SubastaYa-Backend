namespace SubastaYa.Application.UseCases.Auth.Commands;

public record RegisterUserCommand(
    string Nombre,
    string Email,
    string Password,
    string? BaseUrl = null
);

public record RegisterCommand(
    string Nombre,
    string Email,
    string Password,
    string? BaseUrl = null
) : RegisterUserCommand(Nombre, Email, Password, BaseUrl);