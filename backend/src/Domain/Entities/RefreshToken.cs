namespace NasaMoon.Domain.Entities;

/// <summary>
/// A long-lived session token. Only the SHA-256 hash of the token is stored;
/// the raw token is shown to the user once at login/refresh and never again.
/// Rotated on every refresh; revoked on logout.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>SHA-256 hex of the raw refresh token.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
}
