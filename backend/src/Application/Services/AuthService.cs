using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using NasaMoon.Application.Dtos;
using NasaMoon.Application.Interfaces;
using NasaMoon.Domain.Entities;

namespace NasaMoon.Application.Services;

/// <summary>
/// Opaque refresh tokens: random values whose SHA-256 hash is what gets
/// stored. Lives in the Application layer so the auth service never has to
/// reach into Infrastructure (Clean Architecture boundary).
/// </summary>
public static class RefreshTokenHelper
{
    public static string NewRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    /// <summary>Refresh tokens live 7 days (plan §5.2).</summary>
    private const int RefreshTokenDays = 7;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher hasher,
        IJwtTokenService jwt)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
        var displayName = (dto.DisplayName ?? string.Empty).Trim();
        var password = dto.Password ?? string.Empty;

        if (!IsValidEmail(email))
            throw new ArgumentException("A valid email address is required.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.");
        if (password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.");

        if (await _users.GetByEmailAsync(email, ct) is not null)
            throw new InvalidOperationException("An account with this email already exists.");

        // Bootstrap rule: the FIRST registered user becomes Admin.
        var role = await _users.CountAsync(ct) == 0 ? "Admin" : "Researcher";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            PasswordHash = _hasher.Hash(password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
        user = await _users.AddAsync(user, ct);
        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
        var user = await _users.GetByEmailAsync(email, ct);
        if (user is null || !_hasher.Verify(dto.Password ?? string.Empty, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedAccessException("Refresh token is required.");

        var stored = await _refreshTokens.GetByHashAsync(RefreshTokenHelper.HashToken(refreshToken), ct);
        if (stored is null || !stored.IsActive || stored.User is null)
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");

        // Rotate: revoke the old token, issue a fresh pair.
        stored.RevokedAt = DateTime.UtcNow;
        await _refreshTokens.UpdateAsync(stored, ct);
        return await IssueTokensAsync(stored.User, ct);
    }

    public async Task<bool> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return false;
        var stored = await _refreshTokens.GetByHashAsync(RefreshTokenHelper.HashToken(refreshToken), ct);
        if (stored is null || stored.RevokedAt is not null) return false;
        stored.RevokedAt = DateTime.UtcNow;
        return await _refreshTokens.UpdateAsync(stored, ct);
    }

    public async Task<UserDto?> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        return user is null ? null : ToDto(user);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return false;
        if (!_hasher.Verify(dto.CurrentPassword ?? string.Empty, user.PasswordHash)) return false;
        if ((dto.NewPassword ?? string.Empty).Length < 8)
            throw new ArgumentException("New password must be at least 8 characters.");

        user.PasswordHash = _hasher.Hash(dto.NewPassword);
        await _users.UpdateAsync(user, ct);
        // Changing the password logs the user out everywhere.
        await _refreshTokens.RevokeAllForUserAsync(userId, ct);
        return true;
    }

    private async Task<AuthResponseDto> IssueTokensAsync(User user, CancellationToken ct)
    {
        var (accessToken, expiresAt) = _jwt.CreateAccessToken(user);
        var rawRefresh = RefreshTokenHelper.NewRefreshToken();
        await _refreshTokens.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = RefreshTokenHelper.HashToken(rawRefresh),
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
            CreatedAt = DateTime.UtcNow
        }, ct);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefresh,
            AccessTokenExpiresAt = expiresAt,
            User = ToDto(user)
        };
    }

    private static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        DisplayName = u.DisplayName,
        Role = u.Role,
        CreatedAt = u.CreatedAt
    };

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return email.Contains('@');
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _logs;
    private readonly IUserRepository _users;

    public AuditLogService(IAuditLogRepository logs, IUserRepository users)
    {
        _logs = logs;
        _users = users;
    }

    public async Task LogAsync(Guid? userId, string action, string entity, string? entityId, CancellationToken ct = default)
    {
        await _logs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }

    public async Task<List<AuditLogDto>> GetRecentAsync(int count, CancellationToken ct = default)
    {
        var logs = await _logs.GetRecentAsync(count, ct);
        var users = await _users.GetAllAsync(ct);
        var emails = users.ToDictionary(u => u.Id, u => u.Email);
        return logs.Select(l => new AuditLogDto
        {
            Id = l.Id,
            UserId = l.UserId,
            UserEmail = l.UserId.HasValue && emails.TryGetValue(l.UserId.Value, out var e) ? e : l.User?.Email,
            Action = l.Action,
            Entity = l.Entity,
            EntityId = l.EntityId,
            CreatedAt = l.CreatedAt
        }).ToList();
    }
}
