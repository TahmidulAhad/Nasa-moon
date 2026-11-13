using NasaMoon.Application.Dtos;
using NasaMoon.Domain.Entities;

namespace NasaMoon.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task<List<User>> GetAllAsync(CancellationToken ct = default);
    Task<bool> UpdateAsync(User user, CancellationToken ct = default);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task<RefreshToken> AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<bool> UpdateAsync(RefreshToken token, CancellationToken ct = default);
    Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
}

public interface ISavedItemRepository
{
    Task<List<SavedItem>> GetByUserAsync(Guid userId, string? search, CancellationToken ct = default);
    Task<SavedItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SavedItem?> GetByUserAndMissionAsync(Guid userId, string missionId, CancellationToken ct = default);
    Task<SavedItem> AddAsync(SavedItem item, CancellationToken ct = default);
    Task<bool> UpdateAsync(SavedItem item, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface ICollectionRepository
{
    Task<List<Collection>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<Collection?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Collection> AddAsync(Collection collection, CancellationToken ct = default);
    Task<bool> UpdateAsync(Collection collection, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> AddItemAsync(Guid collectionId, string missionId, CancellationToken ct = default);
    Task<bool> RemoveItemAsync(Guid collectionId, string missionId, CancellationToken ct = default);
    Task<List<string>> GetItemMissionIdsAsync(Guid collectionId, CancellationToken ct = default);
}

public interface IAuditLogRepository
{
    Task<AuditLog> AddAsync(AuditLog log, CancellationToken ct = default);
    Task<List<AuditLog>> GetRecentAsync(int count, CancellationToken ct = default);
}

/// <summary>BCrypt password hashing (implemented in Infrastructure).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>JWT access-token creation (implemented in Infrastructure).</summary>
public interface IJwtTokenService
{
    /// <summary>Creates a signed JWT for the user, valid for AccessTokenMinutes.</summary>
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);
}
