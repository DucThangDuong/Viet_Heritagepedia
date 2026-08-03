using System.Threading;
using System.Threading.Tasks;
using API.Endpoints.Locations;
using Application.Interfaces.Repositories;
using FluentValidation.TestHelper;
using Moq;
using Xunit;

namespace UnitTest.Features.Locations;

public class CreateLocationCommandValidatorTests
{
    private readonly CreateLocationRequestValidator _validator;

    public CreateLocationCommandValidatorTests()
    {
        _validator = new CreateLocationRequestValidator();
    }

    [Fact]
    public async Task Should_Have_Error_When_Name_Is_Empty()
    {
        // Arrange
        var command = new CreateLocationRequest { Name = string.Empty, Category = 1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("ERR_LOCATION_NAME_REQUIRED");
    }


    [Fact]
    public async Task Should_Not_Have_Error_When_Name_Is_Unique_And_Valid()
    {
        // Arrange
        var command = new CreateLocationRequest { Name = "New Location", Category = 1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }
}
