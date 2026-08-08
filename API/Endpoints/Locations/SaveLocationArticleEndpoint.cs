using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using API.Extensions;
using Application.Features.Locations.Commands;
using FastEndpoints;
using MediatR;
using System.Security.Claims;

namespace API.Endpoints.Locations;

public class SaveLocationArticleRequest
{
    public Guid LocationId { get; set; }
    
    [FromClaim(ClaimTypes.NameIdentifier)]
    public Guid AuthorId { get; set; }
    
    public JsonElement ContentHtml { get; set; }
}

public class SaveLocationArticleEndpoint : Endpoint<SaveLocationArticleRequest, bool>
{
    private readonly IMediator _mediator;

    public SaveLocationArticleEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/api/locations/{id}/article");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Save main article content for a Location (Manager)";
            s.Description = "Saves HTML content to MongoDB and links it to a Type 1 Contribution";
        });
    }

    public override async Task HandleAsync(SaveLocationArticleRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        if (id != req.LocationId)
        {
            await this.SendApiResponseAsync(Application.Common.Result<bool>.Failure("ERR_ID_MISMATCH", 400), ct);
            return;
        }

        var command = new SaveLocationArticleCommand
        {
            LocationId = req.LocationId,
            AuthorId = req.AuthorId,
            Content = req.ContentHtml
        };

        var result = await _mediator.Send(command, ct);
        await this.SendApiResponseAsync(result, ct);
    }
}
