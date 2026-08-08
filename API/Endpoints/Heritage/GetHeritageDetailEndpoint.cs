using System.Threading;
using System.Threading.Tasks;
using API.Extensions;
using Application.Common;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using FastEndpoints;

namespace API.Endpoints.Heritage;

public class GetHeritageDetailRequest
{
    public string Slug { get; set; } = string.Empty;
}

public class GetHeritageDetailEndpoint : Endpoint<GetHeritageDetailRequest, HeritageDetailDto>
{
    private readonly IHeritageQueryService _queryService;

    public GetHeritageDetailEndpoint(IHeritageQueryService queryService)
    {
        _queryService = queryService;
    }

    public override void Configure()
    {
        Get("/api/heritage-details/{slug}");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting("public_strict"));
        Summary(s =>
        {
            s.Summary = "Get aggregated heritage detail (SQL + MongoDB)";
            s.Description = "Read-only query service for combined SQL metadata and MongoDB rich documents";
        });
    }

    public override async Task HandleAsync(GetHeritageDetailRequest req, CancellationToken ct)
    {
        var dto = await _queryService.GetBySlugAsync(req.Slug, ct);

        if (dto == null)
        {
            var failResult = Result<HeritageDetailDto>.Failure("ERR_DOCUMENT_NOT_FOUND", 404);
            await this.SendApiResponseAsync(failResult, ct);
            return;
        }

        var successResult = Result<HeritageDetailDto>.Success(dto, 200);
        await this.SendApiResponseAsync(successResult, ct);
    }
}
