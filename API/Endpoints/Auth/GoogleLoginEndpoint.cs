using API.Extensions;
using Application.Features.Auth.Commands;
using FastEndpoints;
using FluentValidation;
using MediatR;

namespace API.Endpoints.Auth;

public class GoogleLoginRequest
{
    public string IdToken { get; set; } = string.Empty;
}

public class GoogleLoginRequestValidator : Validator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google IdToken không được để trống");
    }
}

public class GoogleLoginEndpoint : Endpoint<GoogleLoginRequest, API.DTOs.ApiSuccessResponse<Application.DTOs.AuthTokenResponse>>
{
    public IMediator Mediator { get; set; } = null!;

    public override void Configure()
    {
        Post("/api/auth/google");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting("auth_strict"));
        Summary(s =>
        {
            s.Summary = "Login or Register with Google";
            s.Description = "Validates Google Id Token. Creates or links a 'Google' UserAuthProvider. Returns JWT; refresh token is set as HttpOnly cookie.";
        });
    }

    public override async Task HandleAsync(GoogleLoginRequest req, CancellationToken ct)
    {
        var result = await Mediator.Send(new GoogleLoginCommand(req.IdToken), ct);

        if (result.IsSuccess && result.Data is not null)
        {
            HttpContext.Response.Cookies.Append("refreshToken", result.Data.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Expires = result.Data.RefreshTokenExpiryTime,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true
            });
        }

        await this.SendApiResponseAsync(result, ct);
    }
}
