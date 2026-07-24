using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces.QueryServices;

public interface IHeritageQueryService
{
    Task<HeritageDetailDto?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<UserArticleDto?> GetUserArticleByIdAsync(System.Guid id, CancellationToken ct = default);
}
