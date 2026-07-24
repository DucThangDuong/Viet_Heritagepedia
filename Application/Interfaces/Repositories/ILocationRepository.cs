using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ILocationRepository : IRepository<Location>
{
    Task<bool> IsLocationNameUniqueAsync(string name, CancellationToken ct = default);
    Task<Location?> GetByIdWithContributionsAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Location>> GetNearbyLocationsWithPendingContributionsAsync(double latitude, double longitude, double radiusInKm, CancellationToken ct = default);
}
