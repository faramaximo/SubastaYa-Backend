using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SubastaYa.Application.Interfaces;

namespace SubastaYa.WebApi.Services;

public sealed class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration) => _configuration = configuration;

    public string GenerarTokenAcceso(int usuarioId, string nombre, string email)
    {
        var secret = _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey no configurada.");
        var issuer = _configuration["Jwt:Issuer"] ?? "subastaya";
        var audience = _configuration["Jwt:Audience"] ?? "subastaya.frontend";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new Claim(ClaimTypes.Name, nombre),
            new Claim(ClaimTypes.Email, email)
        };
        var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddDays(7), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
