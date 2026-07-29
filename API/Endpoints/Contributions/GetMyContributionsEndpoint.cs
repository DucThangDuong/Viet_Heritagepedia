using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using API.Extensions;
using Application.Common;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace API.Endpoints.Contributions;

public class GetMyContributionsEndpoint : EndpointWithoutRequest<IEnumerable<MyContributionListDto>>
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
        Summary(s =>
        {
            s.Summary = "Get a list of all contributions (drafts, pending, approved) for the current user";
            s.Description = "Returns basic info like Title and Summary, ordered by most recently updated.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var authorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(authorIdClaim, out var authorId) || authorId == Guid.Empty)
        {
            var fail = Result<IEnumerable<MyContributionListDto>>.Failure("ERR_UNAUTHORIZED", 401);
            await this.SendApiResponseAsync(fail, ct);
            return;
        }

        var result = await _queryService.GetMyContributionsAsync(authorId, ct);
        var success = Result<IEnumerable<MyContributionListDto>>.Success(result, 200);
        await this.SendApiResponseAsync(success, ct);
    }
}
