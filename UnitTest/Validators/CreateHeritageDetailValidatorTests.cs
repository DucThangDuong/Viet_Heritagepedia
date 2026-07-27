using System;
using API.Endpoints.Heritage;
using Application.Features.Heritage.Commands;
using FluentValidation.TestHelper;
using Xunit;

namespace Viet_Heritagepedia.Tests.Validators;

public class CreateHeritageDetailValidatorTests
{
    private readonly CreateHeritageDetailValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty()
    {
        var command = new CreateHeritageDetailCommand { Title = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("ERR_TITLE_REQUIRED");
    }

    [Fact]
    public void Should_Have_Error_When_Title_Is_Too_Short()
    {
        var command = new CreateHeritageDetailCommand { Title = "Hue" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("ERR_TITLE_MIN_LENGTH");
    }

    [Fact]
    public void Should_Have_Error_When_Title_Is_Too_Long()
    {
        var command = new CreateHeritageDetailCommand { Title = new string('A', 201) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("ERR_TITLE_MAX_LENGTH");
    }

    [Fact]
    public void Should_Have_Error_When_HistoricalContext_Is_Empty()
    {
        var command = new CreateHeritageDetailCommand { HistoricalContext = "" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.HistoricalContext)
              .WithErrorMessage("ERR_CONTEXT_REQUIRED");
    }

    [Fact]
    public void Should_Have_Error_When_HistoricalContext_Is_Too_Short()
    {
        var command = new CreateHeritageDetailCommand { HistoricalContext = "Ngắn quá" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.HistoricalContext)
              .WithErrorMessage("ERR_CONTEXT_MIN_LENGTH");
    }

    [Fact]
    public void Should_Have_Error_When_LocationId_Is_Empty()
    {
        var command = new CreateHeritageDetailCommand { LocationId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.LocationId)
              .WithErrorMessage("ERR_LOCATION_REQUIRED");
    }

    [Fact]
    public void Should_Have_Error_When_AuthorId_Is_Empty()
    {
        var command = new CreateHeritageDetailCommand { AuthorId = Guid.Empty };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AuthorId)
              .WithErrorMessage("ERR_AUTHOR_REQUIRED");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new CreateHeritageDetailCommand
        {
            Title = "Hoàng Thành Huế",
            HistoricalContext = "Cố đô Huế là quần thể di tích lịch sử văn hóa lâu đời của triều đại nhà Nguyễn.",
            LocationId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid()
        };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
