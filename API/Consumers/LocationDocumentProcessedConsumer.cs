using System;
using System.Threading.Tasks;
using API.Hubs;
using Application.Contracts;
using Domain.Repositories;
using Domain.Entities;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using System.Threading;

namespace API.Consumers;

public class LocationDocumentProcessedConsumer : IConsumer<LocationDocumentProcessedEvent>
{
    private readonly IContributionRepository _contributionRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMongoRepository<HeritageDetailDocument> _mongoRepo;
    private readonly IHubContext<DocumentProcessingHub> _hubContext;

    public LocationDocumentProcessedConsumer(
        IContributionRepository contributionRepo,
        IUnitOfWork unitOfWork,
        IMongoRepository<HeritageDetailDocument> mongoRepo,
        IHubContext<DocumentProcessingHub> hubContext)
    {
        _contributionRepo = contributionRepo;
        _unitOfWork = unitOfWork;
        _mongoRepo = mongoRepo;
        _hubContext = hubContext;
    }

    public async Task Consume(ConsumeContext<LocationDocumentProcessedEvent> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        if (message.Status == "Success")
        {
            var contribution = Contribution.CreateDraft(
                locationId: message.LocationId,
                authorId: message.AuthorId,
                title: $"Tài liệu đóng góp ngày {DateTime.UtcNow:dd/MM/yyyy}",
                summary: null,
                sourceDocumentUrl: null,
                noSqlDocumentId: message.MongoDbId,
                type: Domain.Enums.ContributionType.CommunityArticle
            );

            await _contributionRepo.AddAsync(contribution);
            await _unitOfWork.SaveChangesAsync(ct);

            string jobIdGroup = message.JobId.ToString();
            await _hubContext.Clients.Group(jobIdGroup).SendAsync("ReceiveLocationDocumentResult", new
            {
                JobId = message.JobId,
                LocationId = message.LocationId,
                ContributionId = contribution.Id,
                MongoDbId = message.MongoDbId,
                Status = "Success"
            }, ct);
        }
        else
        {
            string jobIdGroup = message.JobId.ToString();
            await _hubContext.Clients.Group(jobIdGroup).SendAsync("ReceiveLocationDocumentResult", new
            {
                JobId = message.JobId,
                LocationId = message.LocationId,
                Status = "Failed",
                ErrorMessage = message.ErrorMessage
            }, ct);
        }
    }
}
