using System.Threading;
using System.Threading.Tasks;
using API.Extensions;
using Application.DTOs;
using Application.Features.Locations.Commands;
using FastEndpoints;
using MediatR;

namespace API.Endpoints.Locations;

public class CreateLocationEndpoint : Endpoint<CreateLocationCommand, LocationResponseDto>
{
    private readonly IMediator _mediator;

    public CreateLocationEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/api/locations");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a new Location in SQL Server";
            s.Description = "Validates input via FluentValidation and dispatches CreateLocationCommand via MediatR";
        });
    }

    public override async Task HandleAsync(CreateLocationCommand req, CancellationToken ct)
    {
        var result = await _mediator.Send(req, ct);
        await this.SendApiResponseAsync(result, ct);
    }
}
