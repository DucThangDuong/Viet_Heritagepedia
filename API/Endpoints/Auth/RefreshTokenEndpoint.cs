using API.Extensions;
using Application.Features.Auth.Commands;
using FastEndpoints;
using MediatR;
using Microsoft.Extensions.Localization;

namespace API.Endpoints.Auth;

public class RefreshTokenEndpoint : EndpointWithoutRequest
{
    public IMediator Mediator { get; set; } = null!;

    public override void Configure()
    {
        Post("/api/auth/refresh-token");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting("auth_strict"));
        Summary(s =>
        {
            s.Summary = "Refresh Access Token";
            s.Description = "Validates the refresh token against Redis (not SQL). Rotates both tokens on success.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var accessToken = HttpContext.Request.Headers[Microsoft.Net.Http.Headers.HeaderNames.Authorization]
            .FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
        var refreshToken = HttpContext.Request.Cookies["refreshToken"];

        var result = await Mediator.Send(new RefreshTokenCommand(accessToken, refreshToken), ct);

        if (result.IsSuccess && result.Data is not null)
        {
            HttpContext.Response.Cookies.Append("refreshToken", result.Data.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Expires = result.Data.RefreshTokenExpiryTime,
                Secure = true,
                SameSite = SameSiteMode.None,
                IsEssential = true
            });
        }

        await this.SendApiResponseAsync(result, ct);
    }
}
