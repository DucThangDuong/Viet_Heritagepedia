using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Repositories;
using Domain.Enums;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.SqlServer;

public class ContributionRepository : GenericRepository<Contribution>, IContributionRepository
{
    public ContributionRepository(VietHeritagePediaContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Contribution>> GetPendingContributionsByLocationIdAsync(Guid locationId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(c => c.LocationId == locationId && c.WorkflowState == (int)ContributionWorkflowState.PendingReview)
            .ToListAsync(ct);
    }
}
