namespace SubastaYa.Application.UseCases.Auth.Commands;

public record ForgotPasswordCommand(string Email, string BaseUrl);

public record ResetPasswordCommand(string Token, string Password);

public record VerifyEmailCommand(string Token);

public record ResendEmailVerificationCommand(string Email, string BaseUrl);
