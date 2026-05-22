using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AiJobMarketIntelligence.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AiJobMarketIntelligence.Api.Services;

public interface IJwtTokenService
{
    Task<(string Token, DateTime ExpiresAtUtc, string[] Roles)> CreateTokenAsync(ApplicationUser user);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;
    private readonly UserManager<ApplicationUser> _userManager;

    public JwtTokenService(IConfiguration config, UserManager<ApplicationUser> userManager)
    {
        _config = config;
        _userManager = userManager;
    }

    public async Task<(string Token, DateTime ExpiresAtUtc, string[] Roles)> CreateTokenAsync(ApplicationUser user)
    {
        var jwtKey = _config["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT_KEY")
            ?? throw new InvalidOperationException("JWT signing key missing.");

        var issuer = _config["Jwt:Issuer"] ?? Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "AiJobMarketIntelligence";
        var audience = _config["Jwt:Audience"] ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "AiJobMarketIntelligence";

        var expiresAt = DateTime.UtcNow.AddHours(8);

        var roles = (await _userManager.GetRolesAsync(user)).ToArray();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenString, expiresAt, roles);
    }
}
