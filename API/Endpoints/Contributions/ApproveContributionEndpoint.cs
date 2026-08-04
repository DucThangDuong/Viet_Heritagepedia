using System;
using System.Threading;
using System.Threading.Tasks;
using API.Extensions;
using Application.Features.Contributions.Commands;
using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace API.Endpoints.Contributions;

public class ApproveContributionRequest
{
    public bool IsApproved { get; set; }
}

public class ApproveContributionEndpoint : Endpoint<ApproveContributionRequest>
{
    private readonly IMediator _mediator;

    public ApproveContributionEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/api/admin/contributions/{id}/approve");
        AllowAnonymous(); 
        Summary(s =>
        {
            s.Summary = "Approve or reject a contribution (Admin only)";
        });
    }

    public override async Task HandleAsync(ApproveContributionRequest req, CancellationToken ct)
    {
        var contributionId = Route<Guid>("id");

        var command = new ApproveContributionCommand
        {
            ContributionId = contributionId,
            IsApproved = req.IsApproved
        };

        var result = await _mediator.Send(command, ct);
        await this.SendApiResponseAsync(result, ct);
    }
}
