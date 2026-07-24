using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using MongoDB.Bson;

namespace Application.Features.Heritage.Commands;

public class CreateHeritageDetailCommand : IRequest<Result<Guid>>
{
    public string Title { get; set; } = string.Empty;
    public string HistoricalContext { get; set; } = string.Empty;
    public string ArchitectureDetails { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    public Dictionary<string, string> Attributes { get; set; } = new();
    public Guid LocationId { get; set; }
    public Guid AuthorId { get; set; }
}

public class CreateHeritageDetailCommandHandler : IRequestHandler<CreateHeritageDetailCommand, Result<Guid>>
{
    private readonly IMongoRepository<HeritageDetailDocument> _mongoRepo;
    private readonly IContributionRepository _contributionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateHeritageDetailCommandHandler(
        IMongoRepository<HeritageDetailDocument> mongoRepo,
        IContributionRepository contributionRepository,
        IUnitOfWork unitOfWork)
    {
        _mongoRepo = mongoRepo;
        _contributionRepository = contributionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateHeritageDetailCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Result<Guid>.Failure("ERR_TITLE_REQUIRED", 400);
        }

        // 1. Save rich document to MongoDB as a Community Article (Type 2)
        var mongoDoc = new HeritageDetailDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            LocationId = request.LocationId.ToString(),
            ContentHtml = $"<p>{request.HistoricalContext}</p><p>{request.ArchitectureDetails}</p>",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _mongoRepo.InsertAsync(mongoDoc);

        // 2. Save metadata record to SQL Server
        var contribution = new Contribution
        {
            Id = Guid.NewGuid(),
            LocationId = request.LocationId,
            AuthorId = request.AuthorId,
            ContributionType = 2,
            Title = request.Title,
            Summary = request.HistoricalContext.Length > 200 ? request.HistoricalContext[..200] : request.HistoricalContext,
            LikesCount = 0,
            WorkflowState = 1, // Pending
            NoSqlDocumentId = mongoDoc.Id,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _contributionRepository.AddAsync(contribution);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(contribution.Id, 201);
    }
}
