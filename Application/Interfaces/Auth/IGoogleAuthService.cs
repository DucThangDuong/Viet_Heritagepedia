namespace Application.Interfaces.Auth;
public record GoogleUserInfo(string Subject, string Email, string Name, string? Picture);

public interface IGoogleAuthService
{
    Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken, CancellationToken ct = default);
}
