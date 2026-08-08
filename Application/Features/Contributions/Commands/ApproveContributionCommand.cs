using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Domain.Repositories;
using Domain.Entities;
using MediatR;

namespace Application.Features.Contributions.Commands;

public class ApproveContributionCommand : IRequest<Result<Guid>>
{
    public Guid ContributionId { get; set; }
    public bool IsApproved { get; set; }
}

public class ApproveContributionCommandHandler : IRequestHandler<ApproveContributionCommand, Result<Guid>>
{
    private readonly IContributionRepository _contributionRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveContributionCommandHandler(
        IContributionRepository contributionRepo,
        IUnitOfWork unitOfWork)
    {
        _contributionRepo = contributionRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(ApproveContributionCommand request, CancellationToken cancellationToken)
    {
        var contribution = await _contributionRepo.GetByIdAsync(request.ContributionId);

        if (contribution == null)
        {
            return Result<Guid>.Failure("Không tìm thấy bài đóng góp.", 404);
        }

        try
        {
            if (request.IsApproved)
            {
                contribution.Approve();
            }
            else
            {
                contribution.Reject();
            }

            _contributionRepo.Update(contribution);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (ex.GetType().Name.Contains("ConcurrencyException"))
            {
                return Result<Guid>.Failure("ERR_CONCURRENCY_CONFLICT", 409);
            }
            return Result<Guid>.Success(contribution.Id);
        }
        catch (InvalidOperationException ex)
        {
            return Result<Guid>.Failure(ex.Message, 400);
        }
    }
}
