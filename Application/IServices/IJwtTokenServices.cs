using System.Security.Claims;

namespace Application.IServices;

public class RefreshToken
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public DateTime RefreshTokenExpiryTime => ExpiryDate;
}
public record TokenClaims(Guid UserId, string Role, string Jti);

public interface IJWTTokenServices
{
    string GenerateAccessToken(Guid userId, string role);
    RefreshToken GenerateRefreshToken();
    TokenClaims GetClaimsFromToken(string token);
}
