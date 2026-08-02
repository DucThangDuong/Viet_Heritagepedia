using Application.Common;
using Application.DTOs;
using Application.Interfaces.Auth;
using Application.Interfaces.Repositories;
using Application.IServices;
using Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Application.Features.Auth.Commands;

public record GoogleLoginCommand(string IdToken)
    : IRequest<Result<AuthTokenResponse>>;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, Result<AuthTokenResponse>>
{
    private readonly IUserRepository _userRepo;
    private readonly IJWTTokenServices _jwt;
    private readonly ITokenCacheService _tokenCache;
    private readonly IGoogleAuthService _googleAuth;
    private readonly IUnitOfWork _uow;

    public GoogleLoginCommandHandler(
        IUserRepository userRepo,
        IJWTTokenServices jwt,
        ITokenCacheService tokenCache,
        IGoogleAuthService googleAuth,
        IUnitOfWork uow)
    {
        _userRepo = userRepo;
        _jwt = jwt;
        _tokenCache = tokenCache;
        _googleAuth = googleAuth;
        _uow = uow;
    }

    public async Task<Result<AuthTokenResponse>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        var payload = await _googleAuth.ValidateIdTokenAsync(request.IdToken);
        if (payload is null)
            return Result<AuthTokenResponse>.Failure("ERR_INVALID_GOOGLE_TOKEN", 401);

        var existingProvider = await _userRepo.GetAuthProviderAsync("Google", payload.Subject, cancellationToken);

        User user;
        if (existingProvider is not null)
        {
            user = existingProvider.User;
        }
        else
        {
            user = await _userRepo.GetByEmailAsync(payload.Email, cancellationToken)
                   ?? new User
                   {
                       Id = Guid.NewGuid(),
                       FullName = payload.Name ?? payload.Email.Split('@')[0],
                       Email = payload.Email,
                       AvatarUrl = payload.Picture,
                       IsActive = true,
                       IsEmailVerified = true,
                       IsLocked = false,
                       CreatedAt = DateTime.UtcNow,
                       UpdatedAt = DateTime.UtcNow
                   };

            if (existingProvider is null)
                await _userRepo.AddAsync(user);

            var googleProvider = new UserAuthProvider
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ProviderName = "Google",
                ProviderKey = payload.Subject,
                PasswordHash = null,
                CreatedAt = DateTime.UtcNow
            };
            await _userRepo.AddAuthProviderAsync(googleProvider, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        if (!user.IsActive || user.IsLocked)
            return Result<AuthTokenResponse>.Failure("ERR_ACCOUNT_LOCKED", 403);

        var accessToken = _jwt.GenerateAccessToken(user.Id, user.Role ?? "Thành viên");
        var refresh = _jwt.GenerateRefreshToken();
        await _tokenCache.StoreRefreshTokenAsync(
            user.Id, refresh.Token, refresh.ExpiryDate - DateTime.UtcNow, cancellationToken);

        return Result<AuthTokenResponse>.Success(
            new AuthTokenResponse(accessToken, refresh.Token, refresh.ExpiryDate), 200);
    }
}
