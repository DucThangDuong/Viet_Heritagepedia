using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces.Repositories;

namespace Infrastructure.Persistence.SqlServer;

public class UnitOfWork : IUnitOfWork
{
    private readonly VietHeritagePediaContext _context;

    public UnitOfWork(VietHeritagePediaContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
