using System;
using System.Threading;
using System.Threading.Tasks;
using API.Extensions;
using Application.Features.Locations.Commands;
using FastEndpoints;
using MediatR;

namespace API.Endpoints.Locations;

public class DeleteLocationRequest
{
    public Guid Id { get; set; }
}

public class DeleteLocationEndpoint : Endpoint<DeleteLocationRequest, bool>
{
    private readonly IMediator _mediator;

    public DeleteLocationEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Delete("/api/locations/{id}");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Soft delete a Location in SQL Server";
            s.Description = "Dispatches DeleteLocationCommand via MediatR";
        });
    }

    public override async Task HandleAsync(DeleteLocationRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        if (id != req.Id)
        {
            await this.SendApiResponseAsync(Application.Common.Result<bool>.Failure("ERR_ID_MISMATCH", 400), ct);
            return;
        }

        var command = new DeleteLocationCommand { Id = req.Id };
        var result = await _mediator.Send(command, ct);
        
        await this.SendApiResponseAsync(result, ct);
    }
}
