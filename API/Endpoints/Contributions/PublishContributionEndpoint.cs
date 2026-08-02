using System;
using System.Security.Claims;
using FluentValidation;
using System.Threading;
using System.Threading.Tasks;
using API.Extensions;
using Application.Features.Contributions.Commands;
using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace API.Endpoints.Contributions;

public class PublishContributionRequest
{
    public Guid Id { get; set; }

    [FromClaim(ClaimTypes.NameIdentifier)]
    public Guid AuthorId { get; set; }
}

public class PublishContributionRequestValidator : Validator<PublishContributionRequest>
{
    public PublishContributionRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ERR_CONTRIBUTION_ID_REQUIRED");
    }
}

public class PublishContributionEndpoint : Endpoint<PublishContributionRequest>
{
    private readonly IMediator _mediator;

    public PublishContributionEndpoint(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Post("/api/contributions/{Id}/publish");
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Summary(s =>
        {
            s.Summary = "Publish a draft contribution (transitions WorkflowState 0 → 1)";
            s.Description = "Validates rich content stored in MongoDB, changes the SQL WorkflowState " +
                            "to Pending Review, and atomically persists a ContributionSubmittedEvent " +
                            "into the Outbox table via a single UnitOfWork.SaveChangesAsync() call.";
        });
    }

    public override async Task HandleAsync(PublishContributionRequest req, CancellationToken ct)
    {
        var command = new PublishContributionCommand
        {
            ContributionId = req.Id,
            AuthorId = req.AuthorId
        };

        var result = await _mediator.Send(command, ct);
        await this.SendApiResponseAsync(result, ct);
    }
}
