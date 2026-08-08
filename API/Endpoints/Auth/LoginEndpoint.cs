using API.Extensions;
using Application.Features.Auth.Commands;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;

namespace API.Endpoints.Auth;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequestValidator : Validator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .MaximumLength(100).WithMessage("Email quá dài")
            .EmailAddress().WithMessage("Email không đúng định dạng");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống")
            .MinimumLength(6).WithMessage("Mật khẩu phải từ 6 ký tự trở lên")
            .MaximumLength(100).WithMessage("Mật khẩu quá dài");
    }
}

public class LoginEndpoint : Endpoint<LoginRequest, API.DTOs.ApiSuccessResponse<Application.DTOs.AuthTokenResponse>>
{
    public IMediator Mediator { get; set; } = null!;
    public override void Configure()
    {
        Post("/api/auth/login");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting("auth_strict"));
        Summary(s =>
        {
            s.Summary = "Login with email and password";
            s.Description = "Verifies credentials against 'Local' UserAuthProvider. Returns JWT access token; refresh token is set as HttpOnly cookie.";
        });
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var result = await Mediator.Send(new LoginCommand(req.Email, req.Password), ct);

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
