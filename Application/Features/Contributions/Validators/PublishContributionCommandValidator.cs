using System.Threading;
using System.Threading.Tasks;
using Application.Features.Contributions.Commands;
using Application.Interfaces.Repositories;
using Domain.Entities;
using FluentValidation;

namespace Application.Features.Contributions.Validators;

public class PublishContributionCommandValidator : AbstractValidator<PublishContributionCommand>
{
    private readonly IContributionRepository _contributionRepo;
    private readonly IMongoRepository<HeritageDetailDocument> _mongoRepo;

    public PublishContributionCommandValidator(
        IContributionRepository contributionRepo,
        IMongoRepository<HeritageDetailDocument> mongoRepo)
    {
        _contributionRepo = contributionRepo;
        _mongoRepo = mongoRepo;

        RuleFor(x => x.ContributionId)
            .NotEmpty().WithMessage("ERR_CONTRIBUTION_ID_REQUIRED");

        // Validate Business Rules via FluentValidation asynchronously
        RuleFor(x => x)
            .MustAsync(async (command, ct) => 
            {
                var contribution = await _contributionRepo.GetByIdAsync(command.ContributionId);
                return contribution != null;
            })
            .WithMessage("ERR_CONTRIBUTION_NOT_FOUND")
            .DependentRules(() =>
            {
                RuleFor(x => x)
                    .MustAsync(async (command, ct) => 
                    {
                        var contribution = await _contributionRepo.GetByIdAsync(command.ContributionId);
                        return contribution!.AuthorId == command.AuthorId;
                    })
                    .WithMessage("ERR_UNAUTHORIZED_PUBLISH")
                    
                    .MustAsync(async (command, ct) => 
                    {
                        var contribution = await _contributionRepo.GetByIdAsync(command.ContributionId);
                        return contribution!.WorkflowState == 0;
                    })
                    .WithMessage("ERR_CONTRIBUTION_NOT_DRAFT")
                    
                    .MustAsync(async (command, ct) => 
                    {
                        var contribution = await _contributionRepo.GetByIdAsync(command.ContributionId);
                        var mongoDoc = await _mongoRepo.GetByIdAsync(contribution!.NoSqlDocumentId!);
                        
                        if (mongoDoc == null) return false;
                        
                        var contentLength = mongoDoc.ContentHtml?.Length ?? 0;
                        var isTitleValid = !string.IsNullOrWhiteSpace(contribution.Title) && contribution.Title.Length >= 5;
                        var isContentValid = contentLength >= 50;

                        return isTitleValid && isContentValid;
                    })
                    .WithMessage("ERR_INVALID_DOCUMENT_CONTENT");
            });
    }
}
