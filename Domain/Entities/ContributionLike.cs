using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ContributionLike
{
    public Guid ContributionId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime? CreatedAt { get; private set; }

    public virtual Contribution Contribution { get; private set; } = null!;

    public virtual User User { get; private set; } = null!;

    private ContributionLike() { } 

    internal ContributionLike(Guid contributionId, Guid userId)
    {
        ContributionId = contributionId;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
    }
}
