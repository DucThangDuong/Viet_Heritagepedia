using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using API.Extensions;
using Application.Features.Contributions.Commands;
using FastEndpoints;
using FluentValidation;
using MediatR;

namespace API.Endpoints.Contributions;
public class SaveDraftRequest
{
    public Guid? ContributionId { get; set; }
    public Guid LocationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public JsonElement Content { get; set; }
}

public class SaveDraftValidator : Validator<SaveDraftRequest>
{
    public SaveDraftValidator()
    {
        RuleFor(x => x.LocationId)
            .NotEmpty().WithMessage("ERR_LOCATION_REQUIRED");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("ERR_TITLE_REQUIRED")
            .MaximumLength(255).WithMessage("ERR_TITLE_MAX_LENGTH");

        // Ensure Content is a valid JSON object (not an empty token)
        RuleFor(x => x.Content)
            .Must(c => c.ValueKind == JsonValueKind.Object)
            .WithMessage("ERR_CONTENT_INVALID_JSON");
    }
}

public class SaveDraftEndpoint : Endpoint<SaveDraftRequest, SaveDraftResponse>
{
    private readonly IMediator _mediator;

    public SaveDraftEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Post("/api/contributions/drafts");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Upsert a contribution draft (auto-save)";
            s.Description = "Creates a new draft or overwrites an existing one. " +
                            "SQL stores metadata; MongoDB stores the rich JSON content. " +
                            "No integration event is emitted (Outbox is bypassed).";
        });
    }

    public override async Task HandleAsync(SaveDraftRequest req, CancellationToken ct)
    {
        var authorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var authorId = Guid.TryParse(authorIdClaim, out var parsed) ? parsed : Guid.Empty;

        var command = new SaveDraftCommand
        {
            ContributionId = req.ContributionId,
            LocationId = req.LocationId,
            Title = req.Title,
            Content = req.Content,
            AuthorId = authorId
        };

        var result = await _mediator.Send(command, ct);
        await this.SendApiResponseAsync(result, ct);
    }
}
