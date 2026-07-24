using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.SqlServer;

public class LocationRepository : GenericRepository<Location>, ILocationRepository
{
    public LocationRepository(VietHeritagePediaContext context) : base(context)
    {
    }

    public async Task<bool> IsLocationNameUniqueAsync(string name, CancellationToken ct = default)
    {
        return !await _dbSet.AnyAsync(l => l.Name.ToLower() == name.ToLower(), ct);
    }

    public async Task<Location?> GetByIdWithContributionsAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(l => l.Contributions)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public async Task<IEnumerable<Location>> GetNearbyLocationsWithPendingContributionsAsync(
        double latitude, double longitude, double radiusInKm, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(l => l.Contributions.Where(c => c.WorkflowState == 1))
            .Where(l => l.IsActive == true)
            .ToListAsync(ct);
    }
}
