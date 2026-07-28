using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Features.Contributions.Commands;
using Application.Interfaces.Repositories;
using Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTest.Features.Contributions;

public class ContributionHandlersTests
{
    private readonly Mock<IContributionRepository> _contributionRepoMock;
    private readonly Mock<IMongoRepository<HeritageDetailDocument>> _mongoRepoMock;
    private readonly Mock<IRepository<OutboxMessage>> _outboxRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public ContributionHandlersTests()
    {
        _contributionRepoMock = new Mock<IContributionRepository>();
        _mongoRepoMock = new Mock<IMongoRepository<HeritageDetailDocument>>();
        _outboxRepoMock = new Mock<IRepository<OutboxMessage>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    #region SaveDraftCommandHandler Tests

    [Fact]
    public async Task SaveDraft_ShouldCreateNewSqlAndMongoRecords_WhenDraftIdIsNull()
    {
        // Arrange
        var handler = new SaveDraftCommandHandler(_contributionRepoMock.Object, _mongoRepoMock.Object, _unitOfWorkMock.Object);
        var authorId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var rawJson = "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\"}]}";
        
        var command = new SaveDraftCommand
        {
            ContributionId = null,
            LocationId = locationId,
            AuthorId = authorId,
            Title = "New Draft",
            Content = JsonDocument.Parse(rawJson).RootElement
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data.Should().NotBeNull();
        
        _mongoRepoMock.Verify(x => x.InsertAsync(It.Is<HeritageDetailDocument>(d => 
            d.LocationId == locationId.ToString() && 
            d.ContentHtml == rawJson)), Times.Once);
            
        _contributionRepoMock.Verify(x => x.AddAsync(It.Is<Contribution>(c => 
            c.AuthorId == authorId && 
            c.WorkflowState == 0 && 
            c.ContributionType == 2)), Times.Once);
            
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveDraft_ShouldReplaceMongoDocumentAndUpdateSql_WhenDraftIdExistsAndBelongsToUser()
    {
        // Arrange
        var handler = new SaveDraftCommandHandler(_contributionRepoMock.Object, _mongoRepoMock.Object, _unitOfWorkMock.Object);
        var authorId = Guid.NewGuid();
        var existingDraftId = Guid.NewGuid();
        var mongoDbId = "507f1f77bcf86cd799439011";
        
        var existingContribution = new Contribution 
        { 
            Id = existingDraftId, 
            AuthorId = authorId, 
            NoSqlDocumentId = mongoDbId,
            LocationId = Guid.NewGuid()
        };

        _contributionRepoMock.Setup(x => x.GetByIdAsync(existingDraftId)).ReturnsAsync(existingContribution);

        var command = new SaveDraftCommand
        {
            ContributionId = existingDraftId,
            AuthorId = authorId,
            Title = "Updated Draft",
            Content = JsonDocument.Parse("{\"updated\": true}").RootElement
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(200);

        _mongoRepoMock.Verify(x => x.UpdateAsync(mongoDbId, It.IsAny<HeritageDetailDocument>()), Times.Once);
        _contributionRepoMock.Verify(x => x.Update(It.Is<Contribution>(c => c.Title == "Updated Draft")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveDraft_ShouldReturnFailure_WhenUpdatingDraftBelongingToAnotherUser()
    {
        // Arrange
        var handler = new SaveDraftCommandHandler(_contributionRepoMock.Object, _mongoRepoMock.Object, _unitOfWorkMock.Object);
        var existingDraftId = Guid.NewGuid();
        
        var existingContribution = new Contribution 
        { 
            Id = existingDraftId, 
            AuthorId = Guid.NewGuid() // Different user owns this draft
        };

        _contributionRepoMock.Setup(x => x.GetByIdAsync(existingDraftId)).ReturnsAsync(existingContribution);

        var command = new SaveDraftCommand
        {
            ContributionId = existingDraftId,
            AuthorId = Guid.NewGuid(), // Hacker / Current User
            Content = JsonDocument.Parse("{}").RootElement
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ERR_UNAUTHORIZED_DRAFT_ACCESS");
        result.StatusCode.Should().Be(403);
        
        _mongoRepoMock.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<HeritageDetailDocument>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region PublishContributionCommandHandler Tests

    [Fact]
    public async Task Publish_ShouldChangeStateTo1AndAddOutboxMessage_WhenDraftIsValid()
    {
        // Arrange
        var handler = new PublishContributionCommandHandler(
            _contributionRepoMock.Object, 
            _outboxRepoMock.Object, 
            _unitOfWorkMock.Object);

        var authorId = Guid.NewGuid();
        var contributionId = Guid.NewGuid();
        var mongoDbId = "60d5ec49f1165a6f2c3b8b11";

        var draft = new Contribution 
        { 
            Id = contributionId, 
            AuthorId = authorId, 
            WorkflowState = 0, // Draft state
            NoSqlDocumentId = mongoDbId,
            Title = "Valid Title Length"
        };

        _contributionRepoMock.Setup(x => x.GetByIdAsync(contributionId)).ReturnsAsync(draft);

        var command = new PublishContributionCommand
        {
            ContributionId = contributionId,
            AuthorId = authorId
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _contributionRepoMock.Verify(x => x.Update(It.Is<Contribution>(c => c.WorkflowState == 1)), Times.Once);
        _outboxRepoMock.Verify(x => x.AddAsync(It.Is<OutboxMessage>(m => m.MessageType == "ContributionSubmittedEvent")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
