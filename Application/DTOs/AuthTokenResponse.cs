using System;

namespace Application.DTOs;

public record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiryTime
);
