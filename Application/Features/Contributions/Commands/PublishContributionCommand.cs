using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;

namespace Application.Features.Contributions.Commands;

// ───────────────────────────────────────────────────────────────────────────
// Command
// ───────────────────────────────────────────────────────────────────────────
public class PublishContributionCommand : IRequest<Result>
{
    public Guid ContributionId { get; set; }

    // Injected from JWT Claims in the endpoint
    public Guid AuthorId { get; set; }
}

// ───────────────────────────────────────────────────────────────────────────
// Handler
// ───────────────────────────────────────────────────────────────────────────
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
        // ── STEP 1: Fetch validated entity ─────────────────────────────────
        // We guarantee it exists and is valid because FluentValidation passed.
        var contribution = await _contributionRepo.GetByIdAsync(request.ContributionId);

        // ── STEP 2: State Transition (SQL) ────────────────────────────────
        // Move from Draft (0) → Pending Review (1)
        contribution!.WorkflowState = 1;
        contribution.UpdatedAt = DateTime.UtcNow;
        _contributionRepo.Update(contribution);

        // ── STEP 3: Transactional Outbox ──────────────────────────────────
        // Publish the integration event via the Outbox pattern.
        // Both the state update and this Outbox record are flushed in the
        // SAME transaction via UnitOfWork.SaveChangesAsync(), guaranteeing
        // atomicity.
        var outboxEvent = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "ContributionSubmittedEvent",
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                ContributionId = contribution.Id,
                LocationId = contribution.LocationId,
                AuthorId = contribution.AuthorId,
                Title = contribution.Title,
                NoSqlDocumentId = contribution.NoSqlDocumentId,
                SubmittedAt = DateTime.UtcNow
            }),
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null           // null = not yet picked up by the relay
        };

        await _outboxRepo.AddAsync(outboxEvent);

        // Single atomic SaveChanges: WorkflowState change + Outbox record
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(200);
    }
}
