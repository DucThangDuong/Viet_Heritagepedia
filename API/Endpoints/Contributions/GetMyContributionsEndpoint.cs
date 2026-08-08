using System;
using System.Collections.Generic;
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

public class GetMyContributionsRequest
{
    [FromClaim(ClaimTypes.NameIdentifier)]
    public Guid UserId { get; set; }
    
    public int PageIndex { get; set; } = 1;
    
    public int PageSize { get; set; } = 50;
}

public class GetMyContributionsRequestValidator : Validator<GetMyContributionsRequest>
{
    public GetMyContributionsRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("ERR_UNAUTHORIZED_CLAIM");
            
        RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class GetMyContributionsEndpoint : Endpoint<GetMyContributionsRequest, ApiSuccessResponse<IEnumerable<MyContributionListDto>>>
{
    private readonly IContributionQueryService _queryService;

    public GetMyContributionsEndpoint(IContributionQueryService queryService)
    {
        _queryService = queryService;
    }

    public override void Configure()
    {
        Get("/api/my-contributions");
        AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        Options(x => x.RequireRateLimiting("authenticated_strict"));
        Summary(s =>
        {
            s.Summary = "Get a list of all contributions (drafts, pending, approved) for the current user";
            s.Description = "Returns basic info like Title and Summary, ordered by most recently updated.";
        });
    }

    public override async Task HandleAsync(GetMyContributionsRequest req, CancellationToken ct)
    {
        var result = await _queryService.GetMyContributionsAsync(req.UserId, req.PageIndex, req.PageSize, ct);
        var success = Result<IEnumerable<MyContributionListDto>>.Success(result, 200);
        await this.SendApiResponseAsync(success, ct);
    }
}
