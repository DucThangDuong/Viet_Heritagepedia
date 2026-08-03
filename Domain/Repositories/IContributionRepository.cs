using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;

namespace Domain.Repositories;

public interface IContributionRepository : IRepository<Contribution>
{
    Task<IEnumerable<Contribution>> GetPendingContributionsByLocationIdAsync(Guid locationId, CancellationToken ct = default);
}
