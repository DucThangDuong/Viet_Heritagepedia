using Domain.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.SqlServer;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(VietHeritagePediaContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _dbSet
            .Include(u => u.UserAuthProviders)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User?> GetByAuthProviderAsync(string providerName, string providerKey, CancellationToken ct = default)
        => await _dbSet
            .Include(u => u.UserAuthProviders)
            .FirstOrDefaultAsync(u => u.UserAuthProviders.Any(p => p.ProviderName == providerName && p.ProviderKey == providerKey), ct);
}
