namespace Application.Interfaces.Auth;

/// <summary>
/// Validates a Google Id Token and returns the user info extracted from it.
/// The actual Google library lives in Infrastructure; this interface keeps the Application layer clean.
/// </summary>
public record GoogleUserInfo(string Subject, string Email, string Name, string? Picture);

public interface IGoogleAuthService
{
    /// <summary>Returns null if the token is invalid or expired.</summary>
    Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken, CancellationToken ct = default);
}
