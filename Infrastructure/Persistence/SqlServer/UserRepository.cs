using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.SqlServer;

public class UserRepository : IUserRepository
{
    private readonly VietHeritagePediaContext _context;

    public UserRepository(VietHeritagePediaContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Users.FindAsync(new object[] { id }, ct);

    public async Task<UserAuthProvider?> GetAuthProviderAsync(string providerName, string providerKey, CancellationToken ct = default)
        => await _context.UserAuthProviders
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.ProviderName == providerName && p.ProviderKey == providerKey, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _context.Users.AddAsync(user, ct);

    public async Task AddAuthProviderAsync(UserAuthProvider provider, CancellationToken ct = default)
        => await _context.UserAuthProviders.AddAsync(provider, ct);
}
