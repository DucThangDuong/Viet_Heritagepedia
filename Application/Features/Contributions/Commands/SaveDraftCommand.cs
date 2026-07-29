using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using MongoDB.Bson;

namespace Application.Features.Contributions.Commands;
public class SaveDraftCommand : IRequest<Result<SaveDraftResponse>>
{
    public Guid? ContributionId { get; set; }
    public Guid LocationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public JsonElement Content { get; set; }
    public Guid AuthorId { get; set; }
}

public class SaveDraftResponse
{
    public Guid ContributionId { get; set; }
    public string MongoDocumentId { get; set; } = string.Empty;
}
public class SaveDraftCommandHandler : IRequestHandler<SaveDraftCommand, Result<SaveDraftResponse>>
{
    private readonly IContributionRepository _contributionRepo;
    private readonly IMongoRepository<HeritageDetailDocument> _mongoRepo;
    private readonly IUnitOfWork _unitOfWork;
    public SaveDraftCommandHandler(
        IContributionRepository contributionRepo,
        IMongoRepository<HeritageDetailDocument> mongoRepo,
        IUnitOfWork unitOfWork)
    {
        _contributionRepo = contributionRepo;
        _mongoRepo = mongoRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SaveDraftResponse>> Handle(SaveDraftCommand request, CancellationToken ct)
    {
        string mongoId;

        if (request.ContributionId is null)
        {
            var mongoDoc = new HeritageDetailDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                LocationId = request.LocationId.ToString(),
                ContentHtml = request.Content.GetRawText(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _mongoRepo.InsertAsync(mongoDoc);
            mongoId = mongoDoc.Id;
            var contribution = new Contribution
            {
                Id = Guid.NewGuid(),
                LocationId = request.LocationId,
                AuthorId = request.AuthorId,
                ContributionType = 2, 
                Title = request.Title,
                Summary = request.Summary,
                WorkflowState = 0,
                NoSqlDocumentId = mongoId,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _contributionRepo.AddAsync(contribution);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<SaveDraftResponse>.Success(
                new SaveDraftResponse { ContributionId = contribution.Id, MongoDocumentId = mongoId },
                201);
        }
        else
        {
            var contribution = await _contributionRepo.GetByIdAsync(request.ContributionId.Value);
            if (contribution is null)
                return Result<SaveDraftResponse>.Failure("ERR_CONTRIBUTION_NOT_FOUND", 404);

            if (contribution.AuthorId != request.AuthorId)
                return Result<SaveDraftResponse>.Failure("ERR_UNAUTHORIZED_DRAFT_ACCESS", 403);

            mongoId = contribution.NoSqlDocumentId ?? string.Empty;
            
            var existingDoc = await _mongoRepo.GetByIdAsync(mongoId);
            if (existingDoc == null)
            {
                existingDoc = new HeritageDetailDocument
                {
                    Id = mongoId,
                    LocationId = contribution.LocationId.ToString(),
                    CreatedAt = DateTime.UtcNow
                };
            }
            
            existingDoc.ContentHtml = request.Content.GetRawText();
            existingDoc.UpdatedAt = DateTime.UtcNow;
            
            await _mongoRepo.UpdateAsync(mongoId, existingDoc);
            contribution.Title = request.Title;
            contribution.Summary = request.Summary;
            contribution.UpdatedAt = DateTime.UtcNow;
            _contributionRepo.Update(contribution);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<SaveDraftResponse>.Success(
                new SaveDraftResponse { ContributionId = contribution.Id, MongoDocumentId = mongoId },
                200);
        }
    }
}
