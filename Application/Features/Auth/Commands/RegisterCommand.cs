using Application.Common;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Application.Features.Auth.Commands;

public record RegisterCommand(string FullName, string Email, string Password)
    : IRequest<Result<Guid>>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _uow;

    public RegisterCommandHandler(IUserRepository userRepo, IUnitOfWork uow)
    {
        _userRepo = userRepo;
        _uow = uow;
    }

    public async Task<Result<Guid>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userRepo.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            return Result<Guid>.Failure("ERR_EMAIL_ALREADY_EXISTS", 409);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            IsActive = true,
            IsEmailVerified = false,
            IsLocked = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var provider = new UserAuthProvider
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ProviderName = "Local",
            ProviderKey = request.Email,
            PasswordHash = HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.AddAsync(user);
        await _userRepo.AddAuthProviderAsync(provider, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id, 201);
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}
