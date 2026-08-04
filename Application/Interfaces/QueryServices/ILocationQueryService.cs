using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces.QueryServices;

public interface ILocationQueryService
{
    Task<LocationResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<LocationResponseDto>> GetAllLocationsAsync(CancellationToken ct = default);
    Task<List<NearbyLocationDto>> GetNearbyLocationsAsync(double latitude, double longitude, double radiusInKm, CancellationToken ct = default);
}
