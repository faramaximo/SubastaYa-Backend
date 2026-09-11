using System.ComponentModel.DataAnnotations;
namespace SubastaYa.WebApi.Models;
public sealed class EmailRequestDto { [Required, EmailAddress] public string Email { get; init; } = string.Empty; }
public sealed class ResetPasswordDto { [Required] public string Token { get; init; } = string.Empty; [Required, MinLength(12), MaxLength(128)] public string Password { get; init; } = string.Empty; }
