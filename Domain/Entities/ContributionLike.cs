using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ContributionLike
{
    public Guid ContributionId { get; set; }

    public Guid UserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Contribution Contribution { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
