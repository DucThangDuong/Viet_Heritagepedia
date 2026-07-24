using System.Threading;
using System.Threading.Tasks;
using API.Extensions;
using Application.Common;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using FastEndpoints;

namespace API.Endpoints.Heritage;

public class GetHeritageDetailEndpoint : EndpointWithoutRequest<HeritageDetailDto>
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
        Summary(s =>
        {
            s.Summary = "Get aggregated heritage detail (SQL + MongoDB)";
            s.Description = "Read-only query service for combined SQL metadata and MongoDB rich documents";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var slug = Route<string>("slug");
        if (string.IsNullOrEmpty(slug))
        {
            var invalidResult = Result<HeritageDetailDto>.Failure("ERR_INVALID_SLUG", 400);
            await this.SendApiResponseAsync(invalidResult, ct);
            return;
        }

        var dto = await _queryService.GetBySlugAsync(slug, ct);

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
