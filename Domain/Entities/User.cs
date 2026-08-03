using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class User
{
    public Guid Id { get; private set; }

    public string FullName { get; private set; } = null!;

    public string? AvatarUrl { get; private set; }

    public string? Role { get; private set; }

    public string Email { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public bool IsEmailVerified { get; private set; }

    public bool IsLocked { get; private set; }

    public DateTime? LockedUntil { get; private set; }

    public DateTime? LastLoginAt { get; private set; }

    public DateTime? CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    private readonly List<ContributionLike> _contributionLikes = new();
    public virtual IReadOnlyCollection<ContributionLike> ContributionLikes => _contributionLikes.AsReadOnly();

    private readonly List<Contribution> _contributions = new();
    public virtual IReadOnlyCollection<Contribution> Contributions => _contributions.AsReadOnly();

    private readonly List<UserAuthProvider> _userAuthProviders = new();
    public virtual IReadOnlyCollection<UserAuthProvider> UserAuthProviders => _userAuthProviders.AsReadOnly();
}
