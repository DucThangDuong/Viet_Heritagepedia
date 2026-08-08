using System;
using FluentValidation;
using System.Threading;
using System.Threading.Tasks;
using API.DTOs;
using API.Extensions;
using Application.Common;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace API.Endpoints.Contributions;

public class GetAdminContributionDetailRequest
{
    public Guid Id { get; set; }
}

public class GetAdminContributionDetailRequestValidator : Validator<GetAdminContributionDetailRequest>
{
    public GetAdminContributionDetailRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("ERR_INVALID_ID");
    }
}

public class GetAdminContributionDetailEndpoint : Endpoint<GetAdminContributionDetailRequest, ApiSuccessResponse<MyContributionDetailDto>>
{
    private readonly IContributionQueryService _queryService;

    public GetAdminContributionDetailEndpoint(IContributionQueryService queryService)
    {
        _queryService = queryService;
    }

    public override void Configure()
    {
        Get("/api/admin/contributions/{id}");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Get detailed contribution for admin dashboard";
        });
    }

    public override async Task HandleAsync(GetAdminContributionDetailRequest req, CancellationToken ct)
    {
        var result = await _queryService.GetContributionDetailAsync(req.Id, ct);
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
