using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using API.Extensions;
using Application.Common;
using Application.DTOs;
using Application.Interfaces.QueryServices;
using FastEndpoints;
using FluentValidation;

namespace API.Endpoints.Locations;

public class GetNearbyLocationsValidator : Validator<GetNearbyLocationsRequest>
{
    public GetNearbyLocationsValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Vĩ độ phải nằm trong khoảng -90 đến 90");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Kinh độ phải nằm trong khoảng -180 đến 180");

        RuleFor(x => x.RadiusInKm)
            .GreaterThan(0).WithMessage("Bán kính phải lớn hơn 0");
    }
}

public class GetNearbyLocationsEndpoint : Endpoint<GetNearbyLocationsRequest, List<NearbyLocationDto>>
{
    private readonly ILocationQueryService _queryService;

    public GetNearbyLocationsEndpoint(ILocationQueryService queryService)
    {
        _queryService = queryService;
    }

    public override void Configure()
    {
        Get("/api/locations/nearby");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get active locations within radius with pending contributions count";
            s.Description = "Validates input via FluentValidation and executes read-only query service";
        });
    }

    public override async Task HandleAsync(GetNearbyLocationsRequest req, CancellationToken ct)
    {
        var dtoList = await _queryService.GetNearbyLocationsAsync(
            req.Latitude, req.Longitude, req.RadiusInKm, ct);

        var result = Result<List<NearbyLocationDto>>.Success(dtoList, 200);
        await this.SendApiResponseAsync(result, ct);
    }
}
