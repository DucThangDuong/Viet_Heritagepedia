using System.Collections.Generic;
using FluentValidation;
using System.Threading;
using System.Threading.Tasks;
using API.DTOs;
using API.Extensions;
using Application.Common;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using FastEndpoints;

namespace API.Endpoints.Contributions;

public class GetAdminContributionsRequest
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetAdminContributionsRequestValidator : Validator<GetAdminContributionsRequest>
{
    public GetAdminContributionsRequestValidator()
    {
        RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class GetAdminContributionsEndpoint : Endpoint<GetAdminContributionsRequest, ApiSuccessResponse<IEnumerable<MyContributionListDto>>>
{
    private readonly IContributionQueryService _queryService;

    public GetAdminContributionsEndpoint(IContributionQueryService queryService)
    {
        _queryService = queryService;
    }

    public override void Configure()
    {
        Get("/api/admin/contributions");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Get all contributions for admin dashboard";
        });
    }

    public override async Task HandleAsync(GetAdminContributionsRequest req, CancellationToken ct)
    {
        var result = await _queryService.GetAllContributionsAsync(req.PageIndex, req.PageSize, ct);
        var successResult = Result<IEnumerable<MyContributionListDto>>.Success(result, 200);
        await this.SendApiResponseAsync(successResult, ct);
    }
}
