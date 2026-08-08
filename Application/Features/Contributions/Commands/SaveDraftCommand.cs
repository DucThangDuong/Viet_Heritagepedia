using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Domain.Repositories;
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
        var rawContent = request.Content.GetRawText();

        if (request.ContributionId is null)
        {
            var mongoDoc = HeritageDetailDocument.CreateCommunityArticle(
                locationId: request.LocationId.ToString(),
                contentHtml: rawContent
            );
            await _mongoRepo.InsertAsync(mongoDoc);
            mongoId = mongoDoc.Id;
            var contribution = Contribution.CreateDraft(
                locationId: request.LocationId,
                authorId: request.AuthorId,
                title: request.Title,
                summary: request.Summary,
                sourceDocumentUrl: null,
                noSqlDocumentId: mongoId,
                type: Domain.Enums.ContributionType.CommunityArticle
            );

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
                existingDoc = HeritageDetailDocument.CreateCommunityArticle(
                    locationId: contribution.LocationId.ToString(),
                    contentHtml: rawContent
                );
                await _mongoRepo.InsertAsync(existingDoc);
                mongoId = existingDoc.Id;
            }
            else
            {
                existingDoc.UpdateCommunityArticle(request.Content.GetRawText());
                await _mongoRepo.UpdateAsync(mongoId, existingDoc);
            }
            contribution.UpdateContent(
                title: request.Title,
                summary: request.Summary,
                noSqlDocumentId: mongoId
            );
            _contributionRepo.Update(contribution);
            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex.GetType().Name.Contains("ConcurrencyException"))
            {
                return Result<SaveDraftResponse>.Failure("ERR_CONCURRENCY_CONFLICT", 409);
            }

            return Result<SaveDraftResponse>.Success(
                new SaveDraftResponse { ContributionId = contribution.Id, MongoDocumentId = mongoId },
                200);
        }
    }
}
