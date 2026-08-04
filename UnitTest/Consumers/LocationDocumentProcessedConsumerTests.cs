using System;
using System.Threading;
using System.Threading.Tasks;
using API.Consumers;
using API.Hubs;
using Application.Contracts;
using Domain.Repositories;
using Domain.Entities;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace Viet_Heritagepedia.Tests.Consumers;

public class LocationDocumentProcessedConsumerTests
{
    private readonly Mock<IContributionRepository> _contributionRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMongoRepository<HeritageDetailDocument>> _mongoRepoMock;
    private readonly Mock<IHubContext<DocumentProcessingHub>> _hubContextMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly LocationDocumentProcessedConsumer _consumer;

    public LocationDocumentProcessedConsumerTests()
    {
        _contributionRepoMock = new Mock<IContributionRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mongoRepoMock = new Mock<IMongoRepository<HeritageDetailDocument>>();
        
        _hubContextMock = new Mock<IHubContext<DocumentProcessingHub>>();
        var clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        _consumer = new LocationDocumentProcessedConsumer(
            _contributionRepoMock.Object,
            _unitOfWorkMock.Object,
            _mongoRepoMock.Object,
            _hubContextMock.Object
        );
    }

    [Fact]
    public async Task Consume_ShouldCreateContributionAndBroadcastSignalR_WhenSuccessful()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var mongoId = "60d5ec49f1165a6f2c3b8b11";

        var message = new LocationDocumentProcessedEvent
        {
            JobId = jobId,
            LocationId = locationId,
            AuthorId = authorId,
            MongoDbId = mongoId,
            Status = "Success"
        };

        var consumeContextMock = new Mock<ConsumeContext<LocationDocumentProcessedEvent>>();
        consumeContextMock.Setup(c => c.Message).Returns(message);

        var mongoDoc = HeritageDetailDocument.CreateCommunityArticle(locationId.ToString(), "<p>Test Content</p>");

        _mongoRepoMock.Setup(x => x.GetByIdAsync(mongoId))
            .ReturnsAsync(mongoDoc);

        // Act
        await _consumer.Consume(consumeContextMock.Object);

        // Assert
        // 1. Verify SQL Entity is created
        _contributionRepoMock.Verify(x => x.AddAsync(It.Is<Contribution>(c => 
            c.LocationId == locationId &&
            c.AuthorId == authorId &&
            c.NoSqlDocumentId == mongoId &&
            c.ContributionType == 2 // Assuming 2 is Document/Article
        )), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // 2. Removed MongoDB query verification because the payload no longer includes mongoDoc

        // 3. Verify SignalR broadcast
        _clientProxyMock.Verify(x => x.SendCoreAsync("ReceiveLocationDocumentResult", 
            It.Is<object[]>(args => args.Length == 1), 
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
