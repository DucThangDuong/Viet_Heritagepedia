using API.Extensions;
using Application.Common;
using Application.Features.Auth.Commands;
using FastEndpoints;
using MediatR;
using Microsoft.Extensions.Localization;

namespace API.Endpoints.Auth;

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterEndpoint : Endpoint<RegisterRequest>
{
    public IMediator Mediator { get; set; } = null!;
    public IStringLocalizer<SharedResource> Localizer { get; set; } = null!;

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
