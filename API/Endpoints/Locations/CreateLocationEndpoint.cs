using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using API.Extensions;
using Application.DTOs;
using Application.Features.Locations.Commands;
using FastEndpoints;
using MediatR;

namespace API.Endpoints.Locations;

public class CreateLocationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? VietnameseName { get; set; }
    public int Category { get; set; }
    public string Region { get; set; } = "Trung Bộ";
    public string Province { get; set; } = "Thừa Thiên Huế";
    public string? Address { get; set; }
    public bool IsPlainRegion { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public int? UnescoYear { get; set; }
}

public class CreateLocationRequestValidator : Validator<CreateLocationRequest>
{
    public CreateLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("ERR_LOCATION_NAME_REQUIRED")
            .Length(3, 150).WithMessage("Tên địa điểm phải từ 3 đến 150 ký tự")
            .Matches(@"^[\p{L}\p{N}\s.,'-]+$").WithMessage("Tên chứa ký tự không hợp lệ");

        RuleFor(x => x.Slug)
            .Matches(@"^[a-z0-9-]+$").When(x => !string.IsNullOrEmpty(x.Slug)).WithMessage("ERR_INVALID_SLUG_FORMAT")
            .MaximumLength(150);
            
        RuleFor(x => x.VietnameseName)
            .MaximumLength(150);

        RuleFor(x => x.Category)
            .InclusiveBetween(1, 100).WithMessage("Danh mục không hợp lệ");

        RuleFor(x => x.Region).MaximumLength(50);
        RuleFor(x => x.Province).MaximumLength(50);
        RuleFor(x => x.Address).MaximumLength(255);
        RuleFor(x => x.CoverImageUrl).MaximumLength(1000);
    }
}

public class CreateLocationEndpoint : Endpoint<CreateLocationRequest, LocationResponseDto>
{
    private readonly IMediator _mediator;

    public CreateLocationEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/api/locations");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a new Location in SQL Server";
            s.Description = "Validates input via FluentValidation and dispatches CreateLocationCommand via MediatR";
        });
    }

    public override async Task HandleAsync(CreateLocationRequest req, CancellationToken ct)
    {
        var command = new CreateLocationCommand
        {
            Name = req.Name,
            Slug = req.Slug,
            VietnameseName = req.VietnameseName,
            Category = req.Category,
            Region = req.Region,
            Province = req.Province,
            Address = req.Address,
            IsPlainRegion = req.IsPlainRegion,
            CoverImageUrl = req.CoverImageUrl,
            IsFeatured = req.IsFeatured,
            UnescoYear = req.UnescoYear
        };

        var result = await _mediator.Send(command, ct);
        await this.SendApiResponseAsync(result, ct);
    }
}
