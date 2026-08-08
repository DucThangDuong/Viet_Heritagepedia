using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using API.DTOs;
using API.Extensions;
using Application.Common;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace API.Endpoints.Contributions;

public class GetMyContributionDetailRequest
{
    public Guid Id { get; set; }

    [FromClaim(ClaimTypes.NameIdentifier)]
    public Guid UserId { get; set; }
}

public class GetMyContributionDetailRequestValidator : Validator<GetMyContributionDetailRequest>
{
    public GetMyContributionDetailRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ERR_UNAUTHORIZED_CLAIM");
            
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ERR_INVALID_ID");
    }
}

public class GetMyContributionDetailEndpoint : Endpoint<GetMyContributionDetailRequest, ApiSuccessResponse<MyContributionDetailDto>>
{
    private readonly IContributionQueryService _queryService;

    public GetMyContributionDetailEndpoint(IContributionQueryService queryService)
    {
        _queryService = queryService;
    }

    public override void Configure()
    {
        Get("/api/my-contributions/{id}");
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Options(x => x.RequireRateLimiting("authenticated_strict"));
        Summary(s =>
        {
            s.Summary = "Get full details of a specific contribution by the current user";
            s.Description = "Returns all metadata from SQL and rich content from MongoDB. Useful for editing drafts.";
        });
    }

    public override async Task HandleAsync(GetMyContributionDetailRequest req, CancellationToken ct)
    {
        var detail = await _queryService.GetMyContributionDetailAsync(req.UserId, req.Id, ct);
        if (detail == null)
        {
            var fail = Result<MyContributionDetailDto>.Failure("ERR_NOT_FOUND_OR_UNAUTHORIZED", 404);
            await this.SendApiResponseAsync(fail, ct);
            return;
        }

        var success = Result<MyContributionDetailDto>.Success(detail, 200);
        await this.SendApiResponseAsync(success, ct);
    }
}
