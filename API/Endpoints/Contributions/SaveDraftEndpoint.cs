using System.Security.Claims;
using System.Text.Json;
using API.DTOs;
using API.Extensions;
using Application.Common;
using Application.Features.Contributions.Commands;
using FastEndpoints;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace API.Endpoints.Contributions;
public class SaveDraftRequest
{
    public Guid? ContributionId { get; set; }
    public Guid LocationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public JsonElement Content { get; set; }

    [FromClaim(ClaimTypes.NameIdentifier)]
    public Guid AuthorId { get; set; }
}

public class SaveDraftValidator : Validator<SaveDraftRequest>
{
    public SaveDraftValidator()
    {
        RuleFor(x => x.LocationId)
            .NotEmpty().WithMessage("ERR_LOCATION_REQUIRED");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("ERR_TITLE_REQUIRED")
            .MaximumLength(255).WithMessage("ERR_TITLE_MAX_LENGTH")
            .Matches(@"^[\p{L}\p{N}\s.,'-]+$").WithMessage("ERR_TITLE_INVALID_CHARACTERS");
            
        RuleFor(x => x.Summary)
            .MaximumLength(2000).WithMessage("ERR_SUMMARY_MAX_LENGTH");

        RuleFor(x => x.Content)
            .Must(c => c.ValueKind == JsonValueKind.Object)
            .WithMessage("ERR_CONTENT_INVALID_JSON");
            
        RuleFor(x => x.AuthorId)
            .NotEmpty().WithMessage("ERR_UNAUTHORIZED_CLAIM");
    }
}

public class SaveDraftEndpoint : Endpoint<SaveDraftRequest, SaveDraftResponse>
{
    private readonly IMediator _mediator;
    public SaveDraftEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Post("/api/contributions/drafts");
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Summary(s =>
        {
            s.Summary = "Upsert a contribution draft (auto-save)";
            s.Description = "Creates a new draft or overwrites an existing one. " +
                            "Requires authenticated user JWT token. " +
                            "SQL stores metadata; MongoDB stores the rich JSON content.";
        });
        
        Options(x => x.RequireRateLimiting("UploadLimit"));
    }

    public override async Task HandleAsync(SaveDraftRequest req, CancellationToken ct)
    {

        var command = new SaveDraftCommand
        {
            ContributionId = req.ContributionId,
            LocationId = req.LocationId,
            Title = req.Title,
            Summary = req.Summary,
            Content = req.Content,
            AuthorId = req.AuthorId
        };

        var result = await _mediator.Send(command, ct);
        await this.SendApiResponseAsync(result, ct);
    }
}
