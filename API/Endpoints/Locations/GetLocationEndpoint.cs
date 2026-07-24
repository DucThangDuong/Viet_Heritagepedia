using System;
using System.Threading;
using System.Threading.Tasks;
using API.Extensions;
using Application.Common;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using FastEndpoints;

namespace API.Endpoints.Locations;

public class GetLocationEndpoint : EndpointWithoutRequest<LocationResponseDto>
{
    private readonly ILocationQueryService _queryService;

    public GetLocationEndpoint(ILocationQueryService queryService)
    {
        _queryService = queryService;
    }

    public override void Configure()
    {
        Get("/api/locations/{id:guid}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get location by ID using ILocationQueryService";
            s.Description = "Read-only query fetched via ILocationQueryService returning DTO directly";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var dto = await _queryService.GetByIdAsync(id, ct);

        if (dto == null)
        {
            var failResult = Result<LocationResponseDto>.Failure("ERR_LOCATION_NOT_FOUND", 404);
            await this.SendApiResponseAsync(failResult, ct);
            return;
        }

        var successResult = Result<LocationResponseDto>.Success(dto, 200);
        await this.SendApiResponseAsync(successResult, ct);
    }
}
