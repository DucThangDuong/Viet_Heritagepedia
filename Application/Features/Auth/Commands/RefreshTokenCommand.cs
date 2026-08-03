using Application.Common;
using Application.DTOs;
using Application.Interfaces.Auth;
using Domain.Repositories;
using Application.IServices;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Application.Features.Auth.Commands;

public record RefreshTokenCommand(string? AccessToken, string? RefreshToken)
    : IRequest<Result<AuthTokenResponse>>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokenResponse>>
{
    private readonly IJWTTokenServices _jwt;
    private readonly ITokenCacheService _tokenCache;
    private readonly IUserRepository _userRepo;

    public RefreshTokenCommandHandler(IJWTTokenServices jwt, ITokenCacheService tokenCache, IUserRepository userRepo)
    {
        _jwt = jwt;
        _tokenCache = tokenCache;
        _userRepo = userRepo;
    }

    public async Task<Result<AuthTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            return Result<AuthTokenResponse>.Failure("ERR_MISSING_REFRESH_TOKEN", 401);

        var isValid = await _tokenCache.IsRefreshTokenValidAsync(request.RefreshToken, cancellationToken);
        if (!isValid)
            return Result<AuthTokenResponse>.Failure("ERR_INVALID_REFRESH_TOKEN", 401);

        if (string.IsNullOrEmpty(request.AccessToken))
            return Result<AuthTokenResponse>.Failure("ERR_MISSING_ACCESS_TOKEN", 401);

        TokenClaims claims;
        try
        {
            claims = _jwt.GetClaimsFromToken(request.AccessToken);
        }
        catch
        {
            return Result<AuthTokenResponse>.Failure("ERR_INVALID_ACCESS_TOKEN", 401);
        }

        var user = await _userRepo.GetByIdAsync(claims.UserId);
        if (user is null || !user.IsActive || user.IsLocked)
            return Result<AuthTokenResponse>.Failure("ERR_ACCOUNT_LOCKED", 403);

        await _tokenCache.RevokeRefreshTokenAsync(request.RefreshToken, TimeSpan.FromDays(7), cancellationToken);

        var newAccessToken = _jwt.GenerateAccessToken(user.Id, user.Role ?? "Thành viên");
        var newRefresh = _jwt.GenerateRefreshToken();
        await _tokenCache.StoreRefreshTokenAsync(
            user.Id, newRefresh.Token, newRefresh.ExpiryDate - DateTime.UtcNow, cancellationToken);

        return Result<AuthTokenResponse>.Success(
            new AuthTokenResponse(newAccessToken, newRefresh.Token, newRefresh.ExpiryDate), 200);
    }
}
