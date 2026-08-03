using Application.Common;
using Domain.Repositories;
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

        var user = User.Create(
            email: request.Email,
            fullName: request.FullName,
            avatarUrl: null,
            role: "User"
        );

        user.AddAuthProvider("Local", request.Email, HashPassword(request.Password));

        await _userRepo.AddAsync(user);
        await _uow.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(user.Id, 201);
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}
