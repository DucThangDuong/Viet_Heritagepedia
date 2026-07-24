using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? Role { get; set; }

    public string Email { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ContributionLike> ContributionLikes { get; set; } = new List<ContributionLike>();

    public virtual ICollection<Contribution> Contributions { get; set; } = new List<Contribution>();
}
