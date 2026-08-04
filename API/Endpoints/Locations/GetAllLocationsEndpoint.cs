using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Extensions;
using Application.Common;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using FastEndpoints;

namespace API.Endpoints.Locations;

public class GetAllLocationsEndpoint : EndpointWithoutRequest<List<LocationResponseDto>>
{
    private readonly ILocationQueryService _queryService;

    public GetAllLocationsEndpoint(ILocationQueryService queryService)
    {
        _queryService = queryService;
    }

    public override void Configure()
    {
        Get("/api/locations");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get all locations";
            s.Description = "Read-only query fetched via ILocationQueryService returning a list of DTOs";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var locations = await _queryService.GetAllLocationsAsync(ct);
        var successResult = Result<List<LocationResponseDto>>.Success(locations, 200);
        await this.SendApiResponseAsync(successResult, ct);
    }
}
