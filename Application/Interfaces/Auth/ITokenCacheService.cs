namespace Application.Interfaces.Auth;
public interface ITokenCacheService
{
    Task StoreRefreshTokenAsync(Guid userId, string refreshToken, TimeSpan ttl, CancellationToken ct = default);
    Task<bool> IsRefreshTokenValidAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(string refreshToken, TimeSpan ttl, CancellationToken ct = default);
    Task BlacklistAccessTokenAsync(string jti, TimeSpan ttl, CancellationToken ct = default);
    Task<bool> IsAccessTokenBlacklistedAsync(string jti, CancellationToken ct = default);
}
