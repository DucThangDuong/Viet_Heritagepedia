using Application.Interfaces.Auth;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace Infrastructure.Services;

/// <summary>
/// Redis-backed token cache using IDistributedCache.
/// - Refresh tokens are stored with their natural TTL.
/// - Blacklisted tokens (revoked refresh / invalid access JTI) are stored with the prefix "blacklist:".
/// </summary>
public class TokenCacheService : ITokenCacheService
{
    private readonly IDistributedCache _cache;

    // Key prefixes keep the Redis keyspace organized
    private const string RefreshPrefix = "refresh:";
    private const string BlacklistPrefix = "blacklist:";

    public TokenCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    // ── Refresh Token Management ─────────────────────────────────────────────

    /// <summary>
    /// Stores a refresh token in Redis. The key is the token itself, the value
    /// is the UserId so we can audit which user owns it without a DB round-trip.
    /// </summary>
    public async Task StoreRefreshTokenAsync(Guid userId, string refreshToken, TimeSpan ttl, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
        var value = Encoding.UTF8.GetBytes(userId.ToString());
        await _cache.SetAsync(RefreshPrefix + refreshToken, value, options, ct);
    }

    /// <summary>
    /// A refresh token is valid if it EXISTS in Redis (was issued) AND is NOT blacklisted.
    /// </summary>
    public async Task<bool> IsRefreshTokenValidAsync(string refreshToken, CancellationToken ct = default)
    {
        // Must exist (was issued and not expired)
        var exists = await _cache.GetAsync(RefreshPrefix + refreshToken, ct);
        if (exists is null) return false;

        // Must NOT be blacklisted
        var blacklisted = await _cache.GetAsync(BlacklistPrefix + refreshToken, ct);
        return blacklisted is null;
    }

    /// <summary>
    /// Revokes a refresh token by adding it to the blacklist.
    /// The ttl should match the token's remaining life so Redis auto-cleans it.
    /// </summary>
    public async Task RevokeRefreshTokenAsync(string refreshToken, TimeSpan ttl, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
        var value = Encoding.UTF8.GetBytes("revoked");
        await _cache.SetAsync(BlacklistPrefix + refreshToken, value, options, ct);

        // Also remove the valid-token entry to free memory immediately
        await _cache.RemoveAsync(RefreshPrefix + refreshToken, ct);
    }

    // ── Access Token Blacklisting (by JTI) ──────────────────────────────────

    /// <summary>
    /// Blacklists an access token by its JTI claim.
    /// The ttl should be the token's remaining expiry time (~30 min) so Redis auto-cleans it.
    /// </summary>
    public async Task BlacklistAccessTokenAsync(string jti, TimeSpan ttl, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
        var value = Encoding.UTF8.GetBytes("blacklisted");
        await _cache.SetAsync(BlacklistPrefix + "jti:" + jti, value, options, ct);
    }

    /// <summary>
    /// Returns true if the access token's JTI has been blacklisted (e.g., after logout).
    /// </summary>
    public async Task<bool> IsAccessTokenBlacklistedAsync(string jti, CancellationToken ct = default)
    {
        var value = await _cache.GetAsync(BlacklistPrefix + "jti:" + jti, ct);
        return value is not null;
    }
}
