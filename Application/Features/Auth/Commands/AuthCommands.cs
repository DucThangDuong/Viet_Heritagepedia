using Application.Common;
using MediatR;

namespace Application.Features.Auth.Commands;

// ── Response DTO ──────────────────────────────────────────────────────────
public record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiryTime
);

// ═══════════════════════════════════════════════════════════════════════════
// REGISTER COMMAND
// ═══════════════════════════════════════════════════════════════════════════
public record RegisterCommand(string FullName, string Email, string Password)
    : IRequest<Result<Guid>>;

// ═══════════════════════════════════════════════════════════════════════════
// LOGIN COMMAND
// ═══════════════════════════════════════════════════════════════════════════
public record LoginCommand(string Email, string Password)
    : IRequest<Result<AuthTokenResponse>>;

// ═══════════════════════════════════════════════════════════════════════════
// GOOGLE LOGIN COMMAND
// ═══════════════════════════════════════════════════════════════════════════
public record GoogleLoginCommand(string IdToken)
    : IRequest<Result<AuthTokenResponse>>;

// ═══════════════════════════════════════════════════════════════════════════
// REFRESH TOKEN COMMAND (Redis-based blacklist check)
// ═══════════════════════════════════════════════════════════════════════════
public record RefreshTokenCommand(string? AccessToken, string? RefreshToken)
    : IRequest<Result<AuthTokenResponse>>;

// ═══════════════════════════════════════════════════════════════════════════
// LOGOUT COMMAND (Blacklists both tokens in Redis)
// ═══════════════════════════════════════════════════════════════════════════
public record LogoutCommand(Guid UserId, string? AccessToken, string? RefreshToken)
    : IRequest<Result>;
