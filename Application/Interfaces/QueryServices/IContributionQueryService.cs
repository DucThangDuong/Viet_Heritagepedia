using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces.QueryServices;

public interface IContributionQueryService
{
    Task<IEnumerable<MyContributionListDto>> GetMyContributionsAsync(Guid authorId, CancellationToken ct = default);
    Task<MyContributionDetailDto?> GetMyContributionDetailAsync(Guid authorId, Guid contributionId, CancellationToken ct = default);
}
