using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;

namespace Application.Features.Contributions.Commands;
public class PublishContributionCommand : IRequest<Result>
{
    public Guid ContributionId { get; set; }

    public Guid AuthorId { get; set; }
}
public class PublishContributionCommandHandler : IRequestHandler<PublishContributionCommand, Result>
{
    private readonly IContributionRepository _contributionRepo;
    private readonly IRepository<OutboxMessage> _outboxRepo;
    private readonly IUnitOfWork _unitOfWork;

    public PublishContributionCommandHandler(
        IContributionRepository contributionRepo,
        IRepository<OutboxMessage> outboxRepo,
        IUnitOfWork unitOfWork)
    {
        _contributionRepo = contributionRepo;
        _outboxRepo = outboxRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(PublishContributionCommand request, CancellationToken ct)
    {
        var contribution = await _contributionRepo.GetByIdAsync(request.ContributionId);

        contribution!.WorkflowState = 1;
        contribution.UpdatedAt = DateTime.UtcNow;
        _contributionRepo.Update(contribution);
        var outboxEvent = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "ContributionSubmittedEvent",
            Payload = JsonSerializer.Serialize(new
            {
                ContributionId = contribution.Id,
                LocationId = contribution.LocationId,
                AuthorId = contribution.AuthorId,
                Title = contribution.Title,
                NoSqlDocumentId = contribution.NoSqlDocumentId,
                SubmittedAt = DateTime.UtcNow
            }),
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null
        };
        await _outboxRepo.AddAsync(outboxEvent);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(200);
    }
}
