using Application.Common;
using Application.Features.Auth.Commands;
using Application.Interfaces.Auth;
using Application.Interfaces.Repositories;
using Application.IServices;
using Domain.Entities;
using MediatR;
using System.Security.Cryptography;

namespace Application.Features.Auth.Handlers;

// ═══════════════════════════════════════════════════════════════════════════
// REGISTER
// ═══════════════════════════════════════════════════════════════════════════
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _uow;

    public RegisterCommandHandler(IUserRepository userRepo, IUnitOfWork uow)
    {
        _userRepo = userRepo;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userRepo.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            return Result<Guid>.Failure("ERR_EMAIL_ALREADY_EXISTS", 409);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            IsActive = true,
            IsEmailVerified = false,
            IsLocked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var provider = new UserAuthProvider
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ProviderName = "Local",
            ProviderKey = request.Email,
            PasswordHash = HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.AddAsync(user, cancellationToken);
        await _userRepo.AddAuthProviderAsync(provider, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id, 201);
    }

    private static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        string hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
        return $"{Convert.ToBase64String(salt)}.{hash}";
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// LOGIN
// ═══════════════════════════════════════════════════════════════════════════
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
        var provider = await _userRepo.GetAuthProviderAsync("Local", request.Email, cancellationToken);
        if (provider is null || !VerifyPassword(request.Password, provider.PasswordHash))
            return Result<AuthTokenResponse>.Failure("ERR_INVALID_CREDENTIALS", 401);

        var user = provider.User;
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
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;
        byte[] salt = Convert.FromBase64String(parts[0]);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        string hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
        return hash == parts[1];
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// GOOGLE LOGIN — Upsert with Federated Identity
// ═══════════════════════════════════════════════════════════════════════════
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

        // Look up by Google provider key (Google UID = payload.Subject)
        var existingProvider = await _userRepo.GetAuthProviderAsync("Google", payload.Subject, cancellationToken);

        User user;
        if (existingProvider is not null)
        {
            user = existingProvider.User;
        }
        else
        {
            // Check if there's already a Local account with the same email → link it
            user = await _userRepo.GetByEmailAsync(payload.Email, cancellationToken)
                   ?? new User
                   {
                       Id = Guid.NewGuid(),
                       FullName = payload.Name ?? payload.Email.Split('@')[0],
                       Email = payload.Email,
                       AvatarUrl = payload.Picture,
                       IsActive = true,
                       IsEmailVerified = true, // Google verifies email
                       IsLocked = false,
                       CreatedAt = DateTime.UtcNow,
                       UpdatedAt = DateTime.UtcNow
                   };

            if (existingProvider is null)
                await _userRepo.AddAsync(user, cancellationToken);

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

// ═══════════════════════════════════════════════════════════════════════════
// REFRESH TOKEN — Redis blacklist check, no SQL lookup required
// ═══════════════════════════════════════════════════════════════════════════
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

        // 1. Check refresh token is valid (exists in Redis and not blacklisted)
        var isValid = await _tokenCache.IsRefreshTokenValidAsync(request.RefreshToken, cancellationToken);
        if (!isValid)
            return Result<AuthTokenResponse>.Failure("ERR_INVALID_REFRESH_TOKEN", 401);

        // 2. Extract claims from the (possibly expired) access token
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

        var user = await _userRepo.GetByIdAsync(claims.UserId, cancellationToken);
        if (user is null || !user.IsActive || user.IsLocked)
            return Result<AuthTokenResponse>.Failure("ERR_ACCOUNT_LOCKED", 403);

        // 3. Revoke old refresh token (rotation) and issue new pair
        await _tokenCache.RevokeRefreshTokenAsync(request.RefreshToken, TimeSpan.FromDays(7), cancellationToken);

        var newAccessToken = _jwt.GenerateAccessToken(user.Id, user.Role ?? "Thành viên");
        var newRefresh = _jwt.GenerateRefreshToken();
        await _tokenCache.StoreRefreshTokenAsync(
            user.Id, newRefresh.Token, newRefresh.ExpiryDate - DateTime.UtcNow, cancellationToken);

        return Result<AuthTokenResponse>.Success(
            new AuthTokenResponse(newAccessToken, newRefresh.Token, newRefresh.ExpiryDate), 200);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// LOGOUT — Blacklist both tokens in Redis
// ═══════════════════════════════════════════════════════════════════════════
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
        // Blacklist the refresh token
        if (!string.IsNullOrEmpty(request.RefreshToken))
            await _tokenCache.RevokeRefreshTokenAsync(request.RefreshToken, TimeSpan.FromDays(7), cancellationToken);

        // Blacklist the access token JTI so it cannot be reused until expiry
        if (!string.IsNullOrEmpty(request.AccessToken))
        {
            try
            {
                var claims = _jwt.GetClaimsFromToken(request.AccessToken);
                if (!string.IsNullOrEmpty(claims.Jti))
                    await _tokenCache.BlacklistAccessTokenAsync(claims.Jti, TimeSpan.FromMinutes(31), cancellationToken);
            }
            catch { /* Token already invalid — no-op */ }
        }

        return Result.Success(200);
    }
}
