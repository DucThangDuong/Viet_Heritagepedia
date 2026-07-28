using Application.Interfaces.Auth;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services;

/// <summary>
/// Validates Google Id Tokens using the Google.Apis.Auth library.
/// Business logic (user creation/linking) is handled by GoogleLoginCommandHandler.
/// </summary>
public class GoogleAuthService : IGoogleAuthService
{
    private readonly string _googleClientId;

    public GoogleAuthService(IConfiguration configuration)
    {
        _googleClientId = configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Google ClientId not configured.");
    }

    public async Task<GoogleUserInfo?> ValidateIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleClientId }
            };
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return new GoogleUserInfo(payload.Subject, payload.Email, payload.Name, payload.Picture);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
