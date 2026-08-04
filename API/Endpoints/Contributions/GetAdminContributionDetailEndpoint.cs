using System;
using System.Threading;
using System.Threading.Tasks;
using API.Extensions;
using Application.Common;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace API.Endpoints.Contributions;

public class GetAdminContributionDetailEndpoint : EndpointWithoutRequest<MyContributionDetailDto>
{
    private readonly IContributionQueryService _queryService;

    public GetAdminContributionDetailEndpoint(IContributionQueryService queryService)
    {
        _queryService = queryService;
    }

    public override void Configure()
    {
        Get("/api/admin/contributions/{id}");
        AllowAnonymous(); 
        Summary(s =>
        {
            s.Summary = "Get detailed contribution for admin dashboard";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var contributionId = Route<Guid>("id");

        var result = await _queryService.GetContributionDetailAsync(contributionId, ct);
        if (result == null)
        {
            var failResult = Result<MyContributionDetailDto>.Failure("ERR_DOCUMENT_NOT_FOUND", 404);
            await this.SendApiResponseAsync(failResult, ct);
            return;
        }

        var successResult = Result<MyContributionDetailDto>.Success(result, 200);
        await this.SendApiResponseAsync(successResult, ct);
    }
}
