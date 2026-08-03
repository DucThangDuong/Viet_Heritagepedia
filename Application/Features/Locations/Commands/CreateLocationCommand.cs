using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Application.DTOs;
using Domain.Repositories;
using Domain.Entities;
using MediatR;

namespace Application.Features.Locations.Commands;

public class CreateLocationCommand : IRequest<Result<LocationResponseDto>>
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

public class CreateLocationCommandHandler : IRequestHandler<CreateLocationCommand, Result<LocationResponseDto>>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateLocationCommandHandler(
        ILocationRepository locationRepository,
        IUnitOfWork unitOfWork)
    {
        _locationRepository = locationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LocationResponseDto>> Handle(CreateLocationCommand request, CancellationToken cancellationToken)
    {
        var isUnique = await _locationRepository.IsLocationNameUniqueAsync(request.Name, cancellationToken);
        if (!isUnique)
            return Result<LocationResponseDto>.Failure("ERR_LOCATION_NAME_EXISTS", 409);

        var entity = Location.Create(
            name: request.Name,
            slug: request.Slug,
            vietnameseName: request.VietnameseName,
            category: request.Category,
            region: request.Region,
            province: request.Province,
            address: request.Address,
            isPlainRegion: request.IsPlainRegion,
            coverImageUrl: request.CoverImageUrl,
            isFeatured: request.IsFeatured,
            unescoYear: request.UnescoYear
        );

        await _locationRepository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new LocationResponseDto
        {
            Id = entity.Id,
            Slug = entity.Slug,
            Name = entity.Name,
            VietnameseName = entity.VietnameseName,
            Category = entity.Category,
            Region = entity.Region,
            Province = entity.Province,
            Address = entity.Address,
            IsPlainRegion = entity.IsPlainRegion,
            CoverImageUrl = entity.CoverImageUrl,
            IsFeatured = entity.IsFeatured,
            UnescoYear = entity.UnescoYear,
            IsActive = entity.IsActive ?? true,
            CreatedAt = entity.CreatedAt
        };

        return Result<LocationResponseDto>.Success(dto, 201);
    }
}
