using API.Extensions;
using Application.Common;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using FastEndpoints;

namespace API.Endpoints.Articles;

public class GetUserArticleEndpoint : EndpointWithoutRequest<UserArticleDto>
{
    private readonly IHeritageQueryService _queryService;

    public GetUserArticleEndpoint(IHeritageQueryService queryService)
    {
        _queryService = queryService;
    }

    public override void Configure()
    {
        Get("/api/user-articles/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get rich user article document with JSON blocks from MongoDB";
            s.Description = "Returns structured JSON blocks (quotes, timeline, text nodes) for article rendering";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var idStr = Route<string>("id");
        if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var id))
        {
            var fail = Result<UserArticleDto>.Failure("ERR_INVALID_ID", 400);
            await this.SendApiResponseAsync(fail, ct);
            return;
        }

        var article = await _queryService.GetUserArticleByIdAsync(id, ct);

        if (article == null)
        {
            var notFound = Result<UserArticleDto>.Failure("ERR_DOCUMENT_NOT_FOUND", 404);
            await this.SendApiResponseAsync(notFound, ct);
            return;
        }

        var success = Result<UserArticleDto>.Success(article, 200);
        await this.SendApiResponseAsync(success, ct);
    }
}
