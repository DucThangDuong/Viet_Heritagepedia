using Application.Common;
using Application.DTOs;
using Application.Interfaces.Auth;
using Domain.Repositories;
using Application.IServices;
using Domain.Entities;
using MediatR;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Application.Features.Auth.Commands;

public record LoginCommand(string Email, string Password)
    : IRequest<Result<AuthTokenResponse>>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthTokenResponse>>
{
    private readonly IUserRepository _userRepo;
    private readonly IJWTTokenServices _jwt;
    private readonly ITokenCacheService _tokenCache;

    public LoginCommandHandler(IUserRepository userRepo, IJWTTokenServices jwt, ITokenCacheService tokenCache)
    {
        _userRepo = userRepo;
        _jwt = jwt;
        _tokenCache = tokenCache;
    }

    public async Task<Result<AuthTokenResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepo.GetByAuthProviderAsync("Local", request.Email, cancellationToken);
        if (user is null)
            return Result<AuthTokenResponse>.Failure("ERR_INVALID_CREDENTIALS", 401);

        var provider = user.UserAuthProviders.FirstOrDefault(p => p.ProviderName == "Local" && p.ProviderKey == request.Email);
        
        if (provider is null || !VerifyPassword(request.Password, provider.PasswordHash))
            return Result<AuthTokenResponse>.Failure("ERR_INVALID_CREDENTIALS", 401);
        if (!user.IsActive || user.IsLocked)
            return Result<AuthTokenResponse>.Failure("ERR_ACCOUNT_LOCKED", 403);

        return await IssueTokensAsync(user, cancellationToken);
    }

    private async Task<Result<AuthTokenResponse>> IssueTokensAsync(User user, CancellationToken ct)
    {
        var accessToken = _jwt.GenerateAccessToken(user.Id, user.Role ?? "Thành viên");
        var refresh = _jwt.GenerateRefreshToken();

        await _tokenCache.StoreRefreshTokenAsync(
            user.Id, refresh.Token, refresh.ExpiryDate - DateTime.UtcNow, ct);

        return Result<AuthTokenResponse>.Success(
            new AuthTokenResponse(accessToken, refresh.Token, refresh.ExpiryDate), 200);
    }

    private static bool VerifyPassword(string password, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
        catch 
        { 
            return false; 
        }
    }
}
