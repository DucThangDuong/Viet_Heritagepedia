using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Domain.Repositories;
using MediatR;

namespace Application.Features.Locations.Commands;

public class DeleteLocationCommand : IRequest<Result<bool>>
{
    public Guid Id { get; set; }
}

public class DeleteLocationCommandHandler : IRequestHandler<DeleteLocationCommand, Result<bool>>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteLocationCommandHandler(
        ILocationRepository locationRepository,
        IUnitOfWork unitOfWork)
    {
        _locationRepository = locationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
    {
        var entity = await _locationRepository.GetByIdAsync(request.Id);
        if (entity == null)
            return Result<bool>.Failure("ERR_LOCATION_NOT_FOUND", 404);

        entity.Deactivate();

        _locationRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true, 200);
    }
}
