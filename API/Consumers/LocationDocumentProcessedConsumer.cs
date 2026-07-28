using System;
using System.Threading.Tasks;
using API.Hubs;
using Application.Contracts;
using Application.Interfaces.Repositories;
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
            // 1. Create SQL Contribution metadata
            var contribution = new Contribution
            {
                Id = Guid.NewGuid(),
                LocationId = message.LocationId,
                AuthorId = message.AuthorId,
                ContributionType = 2, // 2 = Document/Article from User
                Title = $"Tài liệu đóng góp ngày {DateTime.UtcNow:dd/MM/yyyy}",
                WorkflowState = 0, // Pending
                NoSqlDocumentId = message.MongoDbId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _contributionRepo.AddAsync(contribution);
            await _unitOfWork.SaveChangesAsync(ct);

            // 2. Fetch rich document from MongoDB
            var mongoDoc = await _mongoRepo.GetByIdAsync(message.MongoDbId);

            // 3. Push complete result to the user who uploaded the document
            // We use JobId as the SignalR group since they joined it during upload
            string jobIdGroup = message.JobId.ToString();
            
            await _hubContext.Clients.Group(jobIdGroup).SendAsync("ReceiveLocationDocumentResult", new
            {
                JobId = message.JobId,
                LocationId = message.LocationId,
                ContributionId = contribution.Id,
                Status = "Success",
                ExtractedData = mongoDoc
            }, ct);
        }
        else
        {
            // Handle failure case
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
