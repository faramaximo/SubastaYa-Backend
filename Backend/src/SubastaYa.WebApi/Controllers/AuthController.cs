using Microsoft.AspNetCore.Mvc;
using SubastaYa.Application.DTOs;
using SubastaYa.Application.UseCases.Auth.Commands;
using SubastaYa.Application.UseCases.Auth.Handlers;
using SubastaYa.Application.UseCases.Auth.Queries;

namespace SubastaYa.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RegisterCommandHandler _registerHandler;
    private readonly LoginQueryHandler _loginHandler;
    private readonly IConfiguration _configuration;

    public AuthController(RegisterCommandHandler registerHandler, LoginQueryHandler loginHandler, IConfiguration configuration)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _configuration = configuration;
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

        var response = new LoginResponseDto(user.Id, user.Nombre, user.Email, tokenString);
        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        await _registerHandler.Handle(new RegisterCommand(dto.Nombre, dto.Email, dto.Password));
        return Ok(new { mensaje = "Cuenta creada con éxito." });
    }
}
