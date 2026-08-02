using Application.Interfaces.Auth;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace Infrastructure.Services;
public class TokenCacheService : ITokenCacheService
{
    private readonly IDistributedCache _cache;

    private const string RefreshPrefix = "refresh:";
    private const string BlacklistPrefix = "blacklist:";

    public TokenCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task StoreRefreshTokenAsync(Guid userId, string refreshToken, TimeSpan ttl, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
        var value = Encoding.UTF8.GetBytes(userId.ToString());
        await _cache.SetAsync(RefreshPrefix + refreshToken, value, options, ct);
    }

    public async Task<bool> IsRefreshTokenValidAsync(string refreshToken, CancellationToken ct = default)
    {
        var exists = await _cache.GetAsync(RefreshPrefix + refreshToken, ct);
        if (exists is null) return false;
        var blacklisted = await _cache.GetAsync(BlacklistPrefix + refreshToken, ct);
        return blacklisted is null;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, TimeSpan ttl, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
        var value = Encoding.UTF8.GetBytes("revoked");
        await _cache.SetAsync(BlacklistPrefix + refreshToken, value, options, ct);
        await _cache.RemoveAsync(RefreshPrefix + refreshToken, ct);
    }
    public async Task BlacklistAccessTokenAsync(string jti, TimeSpan ttl, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
        var value = Encoding.UTF8.GetBytes("blacklisted");
        await _cache.SetAsync(BlacklistPrefix + "jti:" + jti, value, options, ct);
    }
    public async Task<bool> IsAccessTokenBlacklistedAsync(string jti, CancellationToken ct = default)
    {
        var value = await _cache.GetAsync(BlacklistPrefix + "jti:" + jti, ct);
        return value is not null;
    }
}
