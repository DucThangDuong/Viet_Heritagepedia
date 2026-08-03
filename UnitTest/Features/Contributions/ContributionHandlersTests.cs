using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Features.Contributions.Commands;
using Domain.Repositories;
using Domain.Enums;
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
        var mongoDbId = "507f1f77bcf86cd799439011";
        var existingContribution = Contribution.CreateDraft(
            locationId: Guid.NewGuid(),
            authorId: authorId,
            title: "Draft Title",
            summary: null,
            sourceDocumentUrl: null,
            noSqlDocumentId: mongoDbId,
            type: Domain.Enums.ContributionType.CommunityArticle
        );
        var existingDraftId = existingContribution.Id;

        _contributionRepoMock.Setup(x => x.GetByIdAsync(existingDraftId)).ReturnsAsync(existingContribution);

        var command = new SaveDraftCommand
        {
            ContributionId = existingDraftId,
            AuthorId = authorId,
            Title = "Updated Draft",
            Content = JsonDocument.Parse("{\"updated\": true}").RootElement
        };

        _mongoRepoMock.Setup(x => x.GetByIdAsync(mongoDbId)).ReturnsAsync(HeritageDetailDocument.CreateCommunityArticle(Guid.NewGuid().ToString(), "html"));

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
        var existingContribution = Contribution.CreateDraft(
            locationId: Guid.NewGuid(),
            authorId: Guid.NewGuid(), // Different user owns this draft
            title: "Draft Title",
            summary: null,
            sourceDocumentUrl: null,
            noSqlDocumentId: null,
            type: Domain.Enums.ContributionType.CommunityArticle
        );
        var existingDraftId = existingContribution.Id;

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
            _unitOfWorkMock.Object,
            _mongoRepoMock.Object);

        var authorId = Guid.NewGuid();
        var mongoDbId = "60d5ec49f1165a6f2c3b8b11";
        var draft = Contribution.CreateDraft(
            locationId: Guid.NewGuid(),
            authorId: authorId,
            title: "Valid Title Length",
            summary: null,
            sourceDocumentUrl: null,
            noSqlDocumentId: mongoDbId,
            type: Domain.Enums.ContributionType.CommunityArticle
        );
        var contributionId = draft.Id;

        _contributionRepoMock.Setup(x => x.GetByIdAsync(contributionId)).ReturnsAsync(draft);
        
        var mongoDoc = HeritageDetailDocument.CreateCommunityArticle(Guid.NewGuid().ToString(), new string('a', 50));
        // Id is generated inside CreateCommunityArticle, but it doesn't matter for the test since we just return it.
        _mongoRepoMock.Setup(x => x.GetByIdAsync(mongoDbId)).ReturnsAsync(mongoDoc);

        var command = new PublishContributionCommand
        {
            ContributionId = contributionId,
            AuthorId = authorId
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _contributionRepoMock.Verify(x => x.Update(It.Is<Contribution>(c => c.WorkflowState == (int)ContributionWorkflowState.PendingReview)), Times.Once);
        _outboxRepoMock.Verify(x => x.AddAsync(It.Is<OutboxMessage>(m => m.MessageType == "ContributionSubmittedEvent")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
