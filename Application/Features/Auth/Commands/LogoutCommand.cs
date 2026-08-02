using Application.Common;
using Application.Interfaces.Auth;
using Application.IServices;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Application.Features.Auth.Commands;

public record LogoutCommand(Guid UserId, string? AccessToken, string? RefreshToken)
    : IRequest<Result>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly ITokenCacheService _tokenCache;
    private readonly IJWTTokenServices _jwt;

    public LogoutCommandHandler(ITokenCacheService tokenCache, IJWTTokenServices jwt)
    {
        _tokenCache = tokenCache;
        _jwt = jwt;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.RefreshToken))
            await _tokenCache.RevokeRefreshTokenAsync(request.RefreshToken, TimeSpan.FromDays(7), cancellationToken);

        if (!string.IsNullOrEmpty(request.AccessToken))
        {
            try
            {
                var claims = _jwt.GetClaimsFromToken(request.AccessToken);
                if (!string.IsNullOrEmpty(claims.Jti))
                    await _tokenCache.BlacklistAccessTokenAsync(claims.Jti, TimeSpan.FromMinutes(31), cancellationToken);
            }
            catch { }
        }

        return Result.Success(200);
    }
}
