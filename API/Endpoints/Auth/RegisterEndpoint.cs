using API.Extensions;
using Application.Common;
using Application.Features.Auth.Commands;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Localization;

namespace API.Endpoints.Auth;

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequestValidator : Validator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống")
            .MinimumLength(2).WithMessage("Họ tên phải từ 2 ký tự trở lên")
            .MaximumLength(50).WithMessage("Họ tên không được vượt quá 50 ký tự");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .MaximumLength(100).WithMessage("Email quá dài")
            .EmailAddress().WithMessage("Email không đúng định dạng");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống")
            .MinimumLength(8).WithMessage("Mật khẩu phải từ 8 ký tự trở lên")
            .MaximumLength(100).WithMessage("Mật khẩu quá dài")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$")
            .WithMessage("Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt");
    }
}

public class RegisterEndpoint : Endpoint<RegisterRequest, API.DTOs.ApiSuccessResponse<System.Guid>>
{
    public IMediator Mediator { get; set; } = null!;

    public override void Configure()
    {
        Post("/api/auth/register");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting("auth_strict"));
        Summary(s =>
        {
            s.Summary = "Register a new user account";
            s.Description = "Creates a User profile and a 'Local' auth provider with a hashed password.";
        });
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var result = await Mediator.Send(new RegisterCommand(req.FullName, req.Email, req.Password), ct);
        await this.SendApiResponseAsync(result, ct);
    }
}
