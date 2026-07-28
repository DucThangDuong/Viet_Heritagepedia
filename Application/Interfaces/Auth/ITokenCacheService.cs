namespace Application.Interfaces.Auth;

/// <summary>
/// Redis-backed token store for managing refresh tokens and blacklisting.
/// Refresh tokens are stored in Redis (not SQL) for high-performance invalidation.
/// </summary>
public interface ITokenCacheService
{
    /// <summary>Stores a refresh token in Redis with TTL matching its expiry.</summary>
    Task StoreRefreshTokenAsync(Guid userId, string refreshToken, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Validates a refresh token: checks it exists and is not blacklisted.</summary>
    Task<bool> IsRefreshTokenValidAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Blacklists a refresh token (on logout or rotation).</summary>
    Task RevokeRefreshTokenAsync(string refreshToken, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Blacklists an access token so it cannot be used even if not expired.</summary>
    Task BlacklistAccessTokenAsync(string jti, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>Checks if a JTI is blacklisted.</summary>
    Task<bool> IsAccessTokenBlacklistedAsync(string jti, CancellationToken ct = default);
}
