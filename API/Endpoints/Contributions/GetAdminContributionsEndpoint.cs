using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Extensions;
using Application.Common;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using FastEndpoints;

namespace API.Endpoints.Contributions;

public class GetAdminContributionsEndpoint : EndpointWithoutRequest<IEnumerable<MyContributionListDto>>
{
    private readonly IContributionQueryService _queryService;

    public GetAdminContributionsEndpoint(IContributionQueryService queryService)
    {
        _queryService = queryService;
    }

    public override void Configure()
    {
        Get("/api/admin/contributions");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get all contributions for admin dashboard";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _queryService.GetAllContributionsAsync(ct);
        var successResult = Result<IEnumerable<MyContributionListDto>>.Success(result, 200);
        await this.SendApiResponseAsync(successResult, ct);
    }
}
