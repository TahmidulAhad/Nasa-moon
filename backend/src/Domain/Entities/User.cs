namespace NasaMoon.Domain.Entities;

/// <summary>
/// A researcher account. Roles: "Researcher" (default) or "Admin".
/// Bootstrap rule: the FIRST registered user automatically becomes Admin.
/// Passwords are stored only as BCrypt hashes — never plain text.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Unique, case-insensitive login id.</summary>
    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>BCrypt hash of the password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>"Researcher" or "Admin".</summary>
    public string Role { get; set; } = "Researcher";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<SavedItem> SavedItems { get; set; } = new List<SavedItem>();
    public ICollection<Collection> Collections { get; set; } = new List<Collection>();
}
