using System.Threading;
using System.Threading.Tasks;
using API.Extensions;
using Application.DTOs;
using Application.Features.Heritage.Commands;
using FastEndpoints;
using FluentValidation;
using MediatR;

namespace API.Endpoints.Heritage;

public class CreateHeritageDetailValidator : Validator<CreateHeritageDetailCommand>
{
    public CreateHeritageDetailValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("ERR_TITLE_REQUIRED");

        RuleFor(x => x.HistoricalContext)
            .NotEmpty().WithMessage("ERR_CONTEXT_REQUIRED");

        RuleFor(x => x.LocationId)
            .NotEmpty().WithMessage("ERR_LOCATION_REQUIRED");

        RuleFor(x => x.AuthorId)
            .NotEmpty().WithMessage("ERR_AUTHOR_REQUIRED");
    }
}

public class CreateHeritageDetailEndpoint : Endpoint<CreateHeritageDetailCommand, Guid>
{
    private readonly IMediator _mediator;

    public CreateHeritageDetailEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/api/heritage-details");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create rich heritage detail document in MongoDB and store metadata in SQL Server";
            s.Description = "Validates input via FluentValidation and dispatches CreateHeritageDetailCommand via MediatR";
        });
    }

    public override async Task HandleAsync(CreateHeritageDetailCommand req, CancellationToken ct)
    {
        var result = await _mediator.Send(req, ct);
        await this.SendApiResponseAsync(result, ct);
    }
}
