using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class UserAuthProvider
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string ProviderName { get; private set; } = null!;

    public string ProviderKey { get; private set; } = null!;

    public string? PasswordHash { get; private set; }

    public DateTime? CreatedAt { get; private set; }

    public DateTime? LastUsedAt { get; private set; }

    public virtual User User { get; private set; } = null!;

    private UserAuthProvider() { }

    internal UserAuthProvider(Guid userId, string providerName, string providerKey, string? passwordHash = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ProviderName = providerName;
        ProviderKey = providerKey;
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
    }
}
