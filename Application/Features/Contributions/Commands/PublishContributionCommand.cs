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
    private readonly IMongoRepository<HeritageDetailDocument> _mongoRepo;

    public PublishContributionCommandHandler(
        IContributionRepository contributionRepo,
        IRepository<OutboxMessage> outboxRepo,
        IUnitOfWork unitOfWork,
        IMongoRepository<HeritageDetailDocument> mongoRepo)
    {
        _contributionRepo = contributionRepo;
        _outboxRepo = outboxRepo;
        _unitOfWork = unitOfWork;
        _mongoRepo = mongoRepo;
    }

    public async Task<Result> Handle(PublishContributionCommand request, CancellationToken ct)
    {
        var contribution = await _contributionRepo.GetByIdAsync(request.ContributionId);
        
        if (contribution == null)
            return Result.Failure("ERR_CONTRIBUTION_NOT_FOUND", 404);

        if (contribution.AuthorId != request.AuthorId)
            return Result.Failure("ERR_UNAUTHORIZED_PUBLISH", 403);

        if (contribution.WorkflowState != 0)
            return Result.Failure("ERR_CONTRIBUTION_NOT_DRAFT", 400);

        var mongoDoc = await _mongoRepo.GetByIdAsync(contribution.NoSqlDocumentId!);
        if (mongoDoc == null)
            return Result.Failure("ERR_INVALID_DOCUMENT_CONTENT", 400);

        var contentLength = mongoDoc.ContentHtml?.Length ?? 0;
        var isTitleValid = !string.IsNullOrWhiteSpace(contribution.Title) && contribution.Title.Length >= 5;
        var isContentValid = contentLength >= 50;

        if (!isTitleValid || !isContentValid)
            return Result.Failure("ERR_INVALID_DOCUMENT_CONTENT", 400);

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
