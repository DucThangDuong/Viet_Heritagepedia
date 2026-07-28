using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Features.Locations.Commands;
using Application.Interfaces.Repositories;
using Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTest.Features.Locations;

public class LocationHandlersTests
{
    private readonly Mock<ILocationRepository> _locationRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateLocationCommandHandler _handler;

    public LocationHandlersTests()
    {
        _locationRepoMock = new Mock<ILocationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new CreateLocationCommandHandler(
            _locationRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateLocationAndReturnSuccess_WhenDataIsValid()
    {
        // Arrange
        var command = new CreateLocationCommand
        {
            Name = "Hoang Thanh Thang Long",
            Slug = "hoang-thanh-thang-long",
            VietnameseName = "Hoàng Thành Thăng Long",
            Category = 1,
            Region = "Bắc Bộ",
            Province = "Hà Nội",
            Address = "19C Hoàng Diệu, Quán Thánh, Ba Đình, Hà Nội",
            IsPlainRegion = true,
            CoverImageUrl = "https://example.com/image.jpg",
            IsFeatured = true,
            UnescoYear = 2010
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Data.Should().NotBeNull();
        result.Data.Name.Should().Be(command.Name);
        result.Data.Slug.Should().Be(command.Slug);
        result.Data.VietnameseName.Should().Be(command.VietnameseName);

        // Verify Repository interactions
        _locationRepoMock.Verify(x => x.AddAsync(It.Is<Location>(l => 
            l.Name == command.Name &&
            l.Slug == command.Slug &&
            l.IsActive == true
        )), Times.Once);

        // Verify UnitOfWork SaveChanges
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldGenerateSlug_WhenSlugIsNull()
    {
        // Arrange
        var command = new CreateLocationCommand
        {
            Name = "Kinh Thanh Hue",
            Slug = null, // Should generate "kinh-thanh-hue"
            Category = 2
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Slug.Should().Be("kinh-thanh-hue");

        _locationRepoMock.Verify(x => x.AddAsync(It.Is<Location>(l => l.Slug == "kinh-thanh-hue")), Times.Once);
    }
}
