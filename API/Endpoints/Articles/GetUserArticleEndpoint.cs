using API.DTOs;
using API.Extensions;
using FluentValidation;
using Application.Common;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using FastEndpoints;

namespace API.Endpoints.Articles;

public class GetUserArticleRequest
{
    public Guid Id { get; set; }
}

public class GetUserArticleRequestValidator : Validator<GetUserArticleRequest>
{
    public GetUserArticleRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("ERR_INVALID_ID");
    }
}

public class GetUserArticleEndpoint : Endpoint<GetUserArticleRequest, ApiSuccessResponse<UserArticleDto>>
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
        Options(x => x.RequireRateLimiting("public_strict"));
        Summary(s =>
        {
            s.Summary = "Get rich user article document with JSON blocks from MongoDB";
            s.Description = "Returns structured JSON blocks (quotes, timeline, text nodes) for article rendering";
        });
    }

    public override async Task HandleAsync(GetUserArticleRequest req, CancellationToken ct)
    {
        var article = await _queryService.GetUserArticleByIdAsync(req.Id, ct);

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
