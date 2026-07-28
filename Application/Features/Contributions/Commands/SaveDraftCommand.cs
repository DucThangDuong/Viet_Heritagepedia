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

// ───────────────────────────────────────────────────────────────────────────
// Command (Application Layer DTO – carries validated intent across the boundary)
// ───────────────────────────────────────────────────────────────────────────
public class SaveDraftCommand : IRequest<Result<SaveDraftResponse>>
{
    /// <summary>Null = new draft, non-null = update existing draft.</summary>
    public Guid? ContributionId { get; set; }
    public Guid LocationId { get; set; }
    public string Title { get; set; } = string.Empty;

    // Dynamic JSON body from TipTap/Editor.js serialised as raw JsonElement
    public JsonElement Content { get; set; }

    // Injected from JWT Claims in the endpoint – never trusted from payload
    public Guid AuthorId { get; set; }
}

public class SaveDraftResponse
{
    public Guid ContributionId { get; set; }
    public string MongoDocumentId { get; set; } = string.Empty;
}

// ───────────────────────────────────────────────────────────────────────────
// Handler
// ───────────────────────────────────────────────────────────────────────────
public class SaveDraftCommandHandler : IRequestHandler<SaveDraftCommand, Result<SaveDraftResponse>>
{
    private readonly IContributionRepository _contributionRepo;
    private readonly IMongoRepository<HeritageDetailDocument> _mongoRepo;
    private readonly IUnitOfWork _unitOfWork;

    // NOTE: We use IUnitOfWork here but we NEVER add an OutboxMessage.
    // The "no integration event" constraint is enforced by the absence of
    // any OutboxMessage entity, NOT by skipping SaveChangesAsync.
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
            // ── NEW DRAFT ──────────────────────────────────────────────────

            // 1. Persist rich JSON content to MongoDB first.
            //    MongoDB is the source-of-truth for the editor's JSON blocks;
            //    SQL only holds lightweight metadata.
            var mongoDoc = new HeritageDetailDocument
            {
                Id = ObjectId.GenerateNewId().ToString(),
                LocationId = request.LocationId.ToString(),
                // Store the dynamic editor blocks as a raw JSON string
                ContentHtml = request.Content.GetRawText(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _mongoRepo.InsertAsync(mongoDoc);
            mongoId = mongoDoc.Id;

            // 2. Persist lightweight metadata row to SQL Server.
            //    WorkflowState = 0 → Draft (never visible to the public)
            var contribution = new Contribution
            {
                Id = Guid.NewGuid(),
                LocationId = request.LocationId,
                AuthorId = request.AuthorId,
                ContributionType = 2,           // Community Article
                Title = request.Title,
                WorkflowState = 0,              // Draft
                NoSqlDocumentId = mongoId,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _contributionRepo.AddAsync(contribution);

            // Save via UnitOfWork — no OutboxMessage is added, so no integration event fires
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<SaveDraftResponse>.Success(
                new SaveDraftResponse { ContributionId = contribution.Id, MongoDocumentId = mongoId },
                201);
        }
        else
        {
            // ── UPDATE DRAFT ───────────────────────────────────────────────

            // 1. Verify ownership in SQL – prevent users from overwriting others' drafts.
            var contribution = await _contributionRepo.GetByIdAsync(request.ContributionId.Value);
            if (contribution is null)
                return Result<SaveDraftResponse>.Failure("ERR_CONTRIBUTION_NOT_FOUND", 404);

            if (contribution.AuthorId != request.AuthorId)
                return Result<SaveDraftResponse>.Failure("ERR_UNAUTHORIZED_DRAFT_ACCESS", 403);

            mongoId = contribution.NoSqlDocumentId ?? string.Empty;

            // 2. Overwrite the MongoDB document completely using ReplaceOneAsync
            //    (wrapped by UpdateAsync in MongoRepository).
            //    This is the correct strategy for editor-style content where the
            //    entire JSON tree is replaced, not patched.
            var updatedDoc = new HeritageDetailDocument
            {
                Id = mongoId,
                LocationId = contribution.LocationId.ToString(),
                ContentHtml = request.Content.GetRawText(),
                UpdatedAt = DateTime.UtcNow
            };
            await _mongoRepo.UpdateAsync(mongoId, updatedDoc);

            // 3. Touch the SQL record's UpdatedAt timestamp only
            contribution.Title = request.Title;
            contribution.UpdatedAt = DateTime.UtcNow;
            _contributionRepo.Update(contribution);

            // Save via UnitOfWork — no OutboxMessage is added, so no integration event fires
            await _unitOfWork.SaveChangesAsync(ct);

            return Result<SaveDraftResponse>.Success(
                new SaveDraftResponse { ContributionId = contribution.Id, MongoDocumentId = mongoId },
                200);
        }
    }
}
