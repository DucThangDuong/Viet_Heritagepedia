using System;
using System.Security.Claims;
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
        var authorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(authorIdClaim, out var authorId) || authorId == Guid.Empty)
        {
            var fail = Application.Common.Result.Failure("ERR_UNAUTHORIZED", 401);
            await this.SendApiResponseAsync(fail, ct);
            return;
        }

        var command = new PublishContributionCommand
        {
            ContributionId = req.Id,
            AuthorId = authorId
        };

        var result = await _mediator.Send(command, ct);
        await this.SendApiResponseAsync(result, ct);
    }
}
