using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserAuthProvider?> GetAuthProviderAsync(string providerName, string providerKey, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task AddAuthProviderAsync(UserAuthProvider provider, CancellationToken ct = default);
}
