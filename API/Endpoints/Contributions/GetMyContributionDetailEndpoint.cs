using System;
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

public class GetMyContributionDetailEndpoint : EndpointWithoutRequest<MyContributionDetailDto>
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
        Summary(s =>
        {
            s.Summary = "Get full details of a specific contribution by the current user";
            s.Description = "Returns all metadata from SQL and rich content from MongoDB. Useful for editing drafts.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var authorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(authorIdClaim, out var authorId) || authorId == Guid.Empty)
        {
            var fail = Result<MyContributionDetailDto>.Failure("ERR_UNAUTHORIZED", 401);
            await this.SendApiResponseAsync(fail, ct);
            return;
        }

        var idStr = Route<string>("id");
        if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var contributionId))
        {
            var fail = Result<MyContributionDetailDto>.Failure("ERR_INVALID_ID", 400);
            await this.SendApiResponseAsync(fail, ct);
            return;
        }

        var detail = await _queryService.GetMyContributionDetailAsync(authorId, contributionId, ct);
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
