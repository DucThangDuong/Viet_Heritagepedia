using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class User
{
    public static User Create(string email, string fullName, string? avatarUrl, string? role)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = fullName,
            AvatarUrl = avatarUrl,
            Role = role ?? "User",
            IsActive = true,
            IsEmailVerified = false,
            IsLocked = false,
            CreatedAt = DateTime.UtcNow
        };

        return user;
    }
    public void VerifyEmail()
    {
        if (IsEmailVerified) return;
        IsEmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }
    public void LockAccount(DateTime until)
    {
        IsLocked = true;
        LockedUntil = until;
        UpdatedAt = DateTime.UtcNow;
    }
    public void UnlockAccount()
    {
        IsLocked = false;
        LockedUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddAuthProvider(string providerName, string providerKey, string? passwordHash = null)
    {
        if (HasAuthProvider(providerName, providerKey)) return;
        _userAuthProviders.Add(new UserAuthProvider(Id, providerName, providerKey, passwordHash));
        UpdatedAt = DateTime.UtcNow;
    }

    public bool HasAuthProvider(string providerName, string providerKey)
    {
        return _userAuthProviders.Exists(p => p.ProviderName == providerName && p.ProviderKey == providerKey);
    }
}
