using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NasaMoon.Application.Interfaces;
using NasaMoon.Domain.Entities;

namespace NasaMoon.Infrastructure.Services;

/// <summary>BCrypt password hashing — plain passwords never touch the database.</summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}

/// <summary>
/// Resolved JWT settings. The secret comes from configuration ("Jwt:Secret"
/// — env var Jwt__Secret on Render, user secrets locally). In Development a
/// missing secret falls back to a random ephemeral one so local dev works;
/// in Production a missing/placeholder secret fails fast.
/// </summary>
public sealed class JwtOptions
{
    public required string Secret { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required int AccessTokenMinutes { get; init; }

    public static JwtOptions FromConfiguration(IConfiguration configuration, bool isDevelopment)
    {
        var secret = configuration["Jwt:Secret"];
        var isPlaceholder = string.IsNullOrWhiteSpace(secret) ||
                            secret.Contains("SET_", StringComparison.OrdinalIgnoreCase) ||
                            secret.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
                            secret.Length < 32;

        if (isPlaceholder && !isDevelopment)
            throw new InvalidOperationException(
                "Jwt:Secret is not configured (set the Jwt__Secret environment variable to a random string of at least 32 characters).");

        return new JwtOptions
        {
            Secret = isPlaceholder
                ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)) // dev-only ephemeral secret
                : secret!,
            Issuer = configuration["Jwt:Issuer"] ?? "NasaMoon",
            Audience = configuration["Jwt:Audience"] ?? "NasaMoonApi",
            AccessTokenMinutes = int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var m) ? m : 60
        };
    }
}

/// <summary>JWT access-token creation.</summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
    }

    public (string Token, DateTime ExpiresAt) CreateAccessToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
