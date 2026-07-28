using System.Security.Claims;

namespace Application.IServices;

public class RefreshToken
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public DateTime RefreshTokenExpiryTime => ExpiryDate;
}

/// <summary>Claims extracted from an access token, usable without taking a JWT library dependency.</summary>
public record TokenClaims(Guid UserId, string Role, string Jti);

public interface IJWTTokenServices
{
    string GenerateAccessToken(Guid userId, string role);
    RefreshToken GenerateRefreshToken();
    /// <summary>Parses an access token (even expired) and returns its claims. Throws if signature is invalid.</summary>
    TokenClaims GetClaimsFromToken(string token);
}
