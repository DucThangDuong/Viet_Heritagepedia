using System;
using System.Text.Json.Serialization;

namespace Application.DTOs;

public record AuthTokenResponse(
    string AccessToken,
    [property: JsonIgnore] string RefreshToken,
    [property: JsonIgnore] DateTime RefreshTokenExpiryTime
);
