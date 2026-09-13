namespace SubastaYa.Application.UseCases.Auth.Commands;

public record ResetPasswordCommand(string Token, string Password);
