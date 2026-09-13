using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.Interfaces;
using SubastaYa.Application.UseCases.Auth.Commands;
using SubastaYa.Application.UseCases.Auth.Handlers;
using SubastaYa.Application.UseCases.Auth.Queries;
using SubastaYa.Infrastructure.Data;
using SubastaYa.WebApi.Models;
using System.Security.Cryptography;
using System.Text;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RegisterUserCommandHandler _registerHandler;
    private readonly LoginQueryHandler _loginHandler;
    private readonly IConfiguration _configuration;
    private readonly SubastaYaDbContext _context;
    private readonly IEmailSender _emailSender;

    public AuthController(RegisterUserCommandHandler registerHandler, LoginQueryHandler loginHandler, IConfiguration configuration, SubastaYaDbContext context, IEmailSender emailSender)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _configuration = configuration;
        _context = context;
        _emailSender = emailSender;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _loginHandler.Handle(new LoginQuery(dto.Email, dto.Password));

        // Generar JWT en WebApi (donde tenemos referencias a Microsoft.IdentityModel)
        var jwtSection = _configuration.GetSection("Jwt");
        var secret = jwtSection["SecretKey"] ?? throw new Exception("JWT SecretKey no configurada");
        var issuer = jwtSection["Issuer"] ?? "subastaya";
        var audience = jwtSection["Audience"] ?? "subastaya.frontend";

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Nombre ?? string.Empty),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email ?? string.Empty)
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        var response = new LoginResponseDto(user.Id, user.Nombre ?? string.Empty, user.Email ?? string.Empty, tokenString);
        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var origin = $"{Request.Scheme}://{Request.Host}";
        await _registerHandler.Handle(new RegisterUserCommand(dto.Nombre, dto.Email, dto.Password, origin), HttpContext.RequestAborted);
        return Ok(new { mensaje = "Cuenta creada. Revisá tu correo para verificarla." });
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var hash = HashToken(token);
        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.TokenVerificacionHash == hash && u.TokenVerificacionExpiraUtc > DateTime.UtcNow);
        if (user is null) return BadRequest(new { error = "El enlace de verificación es inválido o expiró." });
        user.EmailVerificado = true;
        user.TokenVerificacionHash = null;
        user.TokenVerificacionExpiraUtc = null;
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Correo verificado. Ya podés ingresar." });
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] EmailRequestDto dto)
    {
        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is not null && !user.EmailVerificado) await SendVerificationEmailAsync(user, HttpContext.RequestAborted);
        return Ok(new { mensaje = "Si existe una cuenta pendiente, enviamos un correo de verificación." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] EmailRequestDto dto)
    {
        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is not null && user.EmailVerificado)
        {
            var token = CreateToken();
            user.TokenRecuperacionHash = HashToken(token);
            user.TokenRecuperacionExpiraUtc = DateTime.UtcNow.AddMinutes(30);
            await _context.SaveChangesAsync();
            var link = $"{Request.Scheme}://{Request.Host}/pages/reset-password.html?token={Uri.EscapeDataString(token)}";
            await _emailSender.SendAsync(user.Email, "Restablecé tu contraseña", $"<p>Solicitaste restablecer tu contraseña.</p><p><a href=\"{link}\">Restablecer contraseña</a></p><p>Este enlace vence en 30 minutos.</p>", HttpContext.RequestAborted);
        }
        return Ok(new { mensaje = "Si el correo está registrado, recibirás instrucciones para restablecer la contraseña." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.TokenRecuperacionHash == HashToken(dto.Token) && u.TokenRecuperacionExpiraUtc > DateTime.UtcNow);
        if (user is null) return BadRequest(new { error = "El enlace de recuperación es inválido o expiró." });
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        user.TokenRecuperacionHash = null;
        user.TokenRecuperacionExpiraUtc = null;
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Contraseña actualizada. Ya podés ingresar." });
    }

    private async Task SendVerificationEmailAsync(SubastaYa.Domain.Entities.Usuario user, CancellationToken cancellationToken)
    {
        var token = CreateToken();
        user.TokenVerificacionHash = HashToken(token);
        user.TokenVerificacionExpiraUtc = DateTime.UtcNow.AddHours(24);
        await _context.SaveChangesAsync(cancellationToken);
        var link = $"{Request.Scheme}://{Request.Host}/api/auth/verify-email?token={Uri.EscapeDataString(token)}";
        await _emailSender.SendAsync(user.Email, "Verificá tu cuenta", $"<p>Confirmá tu correo para activar la cuenta.</p><p><a href=\"{link}\">Verificar correo</a></p><p>Este enlace vence en 24 horas.</p>", cancellationToken);
    }

    private static string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
