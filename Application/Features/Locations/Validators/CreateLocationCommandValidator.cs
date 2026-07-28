using System.Threading;
using System.Threading.Tasks;
using Application.Features.Locations.Commands;
using Application.Interfaces.Repositories;
using FluentValidation;

namespace Application.Features.Locations.Validators;

public class CreateLocationCommandValidator : AbstractValidator<CreateLocationCommand>
{
    private readonly ILocationRepository _locationRepository;

    public CreateLocationCommandValidator(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("ERR_LOCATION_NAME_REQUIRED")
            .MinimumLength(3).WithMessage("Tên địa điểm phải từ 3 ký tự trở lên")
            .MustAsync(async (name, ct) => 
            {
                if (string.IsNullOrWhiteSpace(name)) return true; // Skip if empty, NotEmpty will catch it
                return await _locationRepository.IsLocationNameUniqueAsync(name, ct);
            }).WithMessage("ERR_LOCATION_NAME_EXISTS");

        RuleFor(x => x.Category)
            .GreaterThan(0).WithMessage("Danh mục không hợp lệ");
    }
}
