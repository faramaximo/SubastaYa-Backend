using SubastaYa.Application.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SubastaYa.WebApi.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerarToken(int userId, string nombre, string email)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var secret = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurada");
        var issuer = jwtSection["Issuer"] ?? "subastaya";
        var audience = jwtSection["Audience"] ?? "subastaya.frontend";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, nombre ?? string.Empty),
            new Claim(ClaimTypes.Email, email ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
