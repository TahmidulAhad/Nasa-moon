using NasaMoon.Application.Dtos;

namespace NasaMoon.Application.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Register a new researcher. The FIRST registered user becomes Admin
    /// (bootstrap rule); everyone after that is a Researcher.
    /// </summary>
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);

    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);

    /// <summary>Rotate the refresh token: the old one is revoked, a new pair is issued.</summary>
    Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Revoke one refresh token (logout on one device).</summary>
    Task<bool> LogoutAsync(string refreshToken, CancellationToken ct = default);

    Task<UserDto?> GetUserAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default);
}

public interface ILibraryService
{
    Task<SavedItemDto> SaveMissionAsync(Guid userId, SaveMissionDto dto, CancellationToken ct = default);
    Task<List<SavedItemDto>> ListSavedAsync(Guid userId, string? search, CancellationToken ct = default);
    Task<SavedItemDto?> UpdateSavedItemAsync(Guid userId, Guid id, UpdateSavedItemDto dto, CancellationToken ct = default);
    Task<bool> DeleteSavedItemAsync(Guid userId, Guid id, CancellationToken ct = default);

    Task<List<CollectionDto>> ListCollectionsAsync(Guid userId, CancellationToken ct = default);
    Task<CollectionDto> CreateCollectionAsync(Guid userId, CreateCollectionDto dto, CancellationToken ct = default);
    Task<CollectionDetailDto?> GetCollectionAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<CollectionDto?> UpdateCollectionAsync(Guid userId, Guid id, UpdateCollectionDto dto, CancellationToken ct = default);
    Task<bool> DeleteCollectionAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<bool> AddToCollectionAsync(Guid userId, Guid collectionId, string missionId, CancellationToken ct = default);
    Task<bool> RemoveFromCollectionAsync(Guid userId, Guid collectionId, string missionId, CancellationToken ct = default);
}

public interface IExportService
{
    /// <summary>Export the caller's whole library (saved items + collections + notes/tags).</summary>
    Task<(byte[] Bytes, string ContentType, string FileName)> ExportLibraryAsync(Guid userId, string format, CancellationToken ct = default);

    Task<(byte[] Bytes, string ContentType, string FileName)?> ExportCollectionAsync(Guid userId, Guid collectionId, string format, CancellationToken ct = default);

    /// <summary>Export one snapshot's missions. Caller must be able to see the snapshot.</summary>
    Task<(byte[] Bytes, string ContentType, string FileName)?> ExportSnapshotAsync(Guid snapshotId, string format, CancellationToken ct = default);
}

public interface IAuditLogService
{
    Task LogAsync(Guid? userId, string action, string entity, string? entityId, CancellationToken ct = default);
    Task<List<AuditLogDto>> GetRecentAsync(int count, CancellationToken ct = default);
}
