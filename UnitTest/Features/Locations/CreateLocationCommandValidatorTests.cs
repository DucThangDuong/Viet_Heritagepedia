using System.Threading;
using System.Threading.Tasks;
using Application.Features.Locations.Commands;
using Application.Features.Locations.Validators;
using Application.Interfaces.Repositories;
using FluentValidation.TestHelper;
using Moq;
using Xunit;

namespace UnitTest.Features.Locations;

public class CreateLocationCommandValidatorTests
{
    private readonly Mock<ILocationRepository> _locationRepoMock;
    private readonly CreateLocationCommandValidator _validator;

    public CreateLocationCommandValidatorTests()
    {
        _locationRepoMock = new Mock<ILocationRepository>();
        _validator = new CreateLocationCommandValidator(_locationRepoMock.Object);
    }

    [Fact]
    public async Task Should_Have_Error_When_Name_Is_Empty()
    {
        // Arrange
        var command = new CreateLocationCommand { Name = string.Empty, Category = 1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("ERR_LOCATION_NAME_REQUIRED");
    }

    [Fact]
    public async Task Should_Have_Error_When_Name_Is_Not_Unique()
    {
        // Arrange
        var command = new CreateLocationCommand { Name = "Existing Location", Category = 1 };
        
        _locationRepoMock
            .Setup(x => x.IsLocationNameUniqueAsync("Existing Location", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("ERR_LOCATION_NAME_EXISTS");
    }

    [Fact]
    public async Task Should_Not_Have_Error_When_Name_Is_Unique_And_Valid()
    {
        // Arrange
        var command = new CreateLocationCommand { Name = "New Location", Category = 1 };
        
        _locationRepoMock
            .Setup(x => x.IsLocationNameUniqueAsync("New Location", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }
}
