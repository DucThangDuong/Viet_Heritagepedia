using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Domain.Repositories;
using Domain.Entities;
using MediatR;
using System.Linq;

namespace Application.Features.Locations.Commands;

public class SaveLocationArticleCommand : IRequest<Result<bool>>
{
    public Guid LocationId { get; set; }
    public Guid AuthorId { get; set; } // Manager's ID
    public JsonElement Content { get; set; } // HTML/JSON from Editor
}

public class SaveLocationArticleCommandHandler : IRequestHandler<SaveLocationArticleCommand, Result<bool>>
{
    private readonly ILocationRepository _locationRepo;
    private readonly IContributionRepository _contributionRepo;
    private readonly IMongoRepository<HeritageDetailDocument> _mongoRepo;
    private readonly IUnitOfWork _unitOfWork;

    public SaveLocationArticleCommandHandler(
        ILocationRepository locationRepo,
        IContributionRepository contributionRepo,
        IMongoRepository<HeritageDetailDocument> mongoRepo,
        IUnitOfWork unitOfWork)
    {
        _locationRepo = locationRepo;
        _contributionRepo = contributionRepo;
        _mongoRepo = mongoRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(SaveLocationArticleCommand request, CancellationToken ct)
    {
        var location = await _locationRepo.GetByIdAsync(request.LocationId);
        if (location == null)
            return Result<bool>.Failure("ERR_LOCATION_NOT_FOUND", 404);
        var existingContributions = await _contributionRepo.FindAsync(c => c.LocationId == request.LocationId);
        var mainContribution = existingContributions.FirstOrDefault(c => c.TypeEnum == Domain.Enums.ContributionType.MainContent);

        string htmlContent = request.Content.GetRawText();
        string mongoId;

        if (mainContribution == null)
        {
            // 1. Create Mongo Document
            var mongoDoc = HeritageDetailDocument.CreateMainArticle(
                locationId: request.LocationId.ToString(),
                fullDescription: htmlContent
            );
            await _mongoRepo.InsertAsync(mongoDoc);
            mongoId = mongoDoc.Id;

            // 2. Create Contribution (Type 1, Approved)
            mainContribution = Contribution.CreateMainArticle(
                locationId: request.LocationId,
                authorId: request.AuthorId,
                title: location.Name + " - Main Article",
                noSqlDocumentId: mongoId
            );

            await _contributionRepo.AddAsync(mainContribution);
        }
        else
        {
            mongoId = mainContribution.NoSqlDocumentId ?? string.Empty;
            var existingDoc = await _mongoRepo.GetByIdAsync(mongoId);

            if (existingDoc == null)
            {
                existingDoc = HeritageDetailDocument.CreateMainArticle(
                    locationId: request.LocationId.ToString(),
                    fullDescription: htmlContent
                );
                await _mongoRepo.InsertAsync(existingDoc);
                mongoId = existingDoc.Id;
            }
            else
            {
                existingDoc.UpdateMainArticle(htmlContent);
                await _mongoRepo.UpdateAsync(mongoId, existingDoc);
            }

            mainContribution.UpdateMainContent(mongoId);
            _contributionRepo.Update(mainContribution);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true, 200);
    }
}
