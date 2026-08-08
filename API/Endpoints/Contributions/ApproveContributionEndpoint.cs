using System;
using FluentValidation;
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
    public Guid Id { get; set; }
    public bool IsApproved { get; set; }
}

public class ApproveContributionRequestValidator : Validator<ApproveContributionRequest>
{
    public ApproveContributionRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("ERR_INVALID_ID");
    }
}

public class ApproveContributionEndpoint : Endpoint<ApproveContributionRequest, API.DTOs.ApiSuccessResponse<Guid>>
{
    private readonly IMediator _mediator;

    public ApproveContributionEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/api/admin/contributions/{id}/approve");
        Roles("Admin");
        AuthSchemes(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);
        Options(x => x.RequireRateLimiting("admin_strict"));
        Summary(s =>
        {
            s.Summary = "Approve or reject a contribution (Admin only)";
        });
    }

    public override async Task HandleAsync(ApproveContributionRequest req, CancellationToken ct)
    {
        var command = new ApproveContributionCommand
        {
            ContributionId = req.Id,
            IsApproved = req.IsApproved
        };

        var result = await _mediator.Send(command, ct);
        await this.SendApiResponseAsync(result, ct);
    }
}
