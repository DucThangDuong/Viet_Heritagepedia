using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class UserAuthProvider
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string ProviderName { get; set; } = null!;

    public string ProviderKey { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
