using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Application.DTOs;
using Domain.Repositories;
using Domain.Entities;
using MediatR;

namespace Application.Features.Locations.Commands;

public class UpdateLocationCommand : IRequest<Result<LocationResponseDto>>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? VietnameseName { get; set; }
    public int Category { get; set; }
    public string Region { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsPlainRegion { get; set; }
    public string? CoverImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public int? UnescoYear { get; set; }
}

public class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand, Result<LocationResponseDto>>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLocationCommandHandler(
        ILocationRepository locationRepository,
        IUnitOfWork unitOfWork)
    {
        _locationRepository = locationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LocationResponseDto>> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var entity = await _locationRepository.GetByIdAsync(request.Id);
        if (entity == null)
            return Result<LocationResponseDto>.Failure("ERR_LOCATION_NOT_FOUND", 404);

        if (entity.Name != request.Name)
        {
            var isUnique = await _locationRepository.IsLocationNameUniqueAsync(request.Name, cancellationToken);
            if (!isUnique)
                return Result<LocationResponseDto>.Failure("ERR_LOCATION_NAME_EXISTS", 409);
        }

        entity.UpdateFull(
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

        _locationRepository.Update(entity);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().Name.Contains("ConcurrencyException"))
        {
            return Result<LocationResponseDto>.Failure("ERR_CONCURRENCY_CONFLICT", 409);
        }

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

        return Result<LocationResponseDto>.Success(dto, 200);
    }
}
