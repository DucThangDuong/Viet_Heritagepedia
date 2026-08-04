using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using API.Extensions;
using Application.DTOs;
using Application.Features.Locations.Commands;
using FastEndpoints;
using MediatR;

namespace API.Endpoints.Locations;

public class UpdateLocationRequest
{
    public Guid Id { get; set; }
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

public class UpdateLocationRequestValidator : Validator<UpdateLocationRequest>
{
    public UpdateLocationRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("ID không được để trống");

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

public class UpdateLocationEndpoint : Endpoint<UpdateLocationRequest, LocationResponseDto>
{
    private readonly IMediator _mediator;

    public UpdateLocationEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Put("/api/locations/{id}");
        Authorize(Roles = "Admin");
        Summary(s =>
        {
            s.Summary = "Update an existing Location in SQL Server";
            s.Description = "Validates input via FluentValidation and dispatches UpdateLocationCommand via MediatR";
        });
    }

    public override async Task HandleAsync(UpdateLocationRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        if (id != req.Id)
        {
            await this.SendApiResponseAsync(Application.Common.Result<LocationResponseDto>.Failure("ERR_ID_MISMATCH", 400), ct);
            return;
        }

        var command = new UpdateLocationCommand
        {
            Id = req.Id,
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
