using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.SqlServer;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(VietHeritagePediaContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<UserAuthProvider?> GetAuthProviderAsync(string providerName, string providerKey, CancellationToken ct = default)
        => await _context.UserAuthProviders
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.ProviderName == providerName && p.ProviderKey == providerKey, ct);

    public async Task AddAuthProviderAsync(UserAuthProvider provider, CancellationToken ct = default)
        => await _context.UserAuthProviders.AddAsync(provider, ct);
}
