using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Auth.Dtos;
using CareerCopilot.Domain.Entities;
using CareerCopilot.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Auth.Register;

public sealed record RegisterCommand(string Email, string Password, string FullName) : IRequest<AuthResponse>;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public RegisterCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var exists = await _db.Set<User>()
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (exists)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.User,
            IsActive = true
        };

        _db.Add(user);

        var profile = new UserProfile
        {
            UserId = user.Id,
            FullName = request.FullName.Trim(),
            Email = email
        };

        _db.Add(profile);

        await _db.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenService.CreateToken(user.Id, user.Email, user.Role);

        return new AuthResponse(token, new UserDto(user.Id, user.Email, user.Role.ToString(), user.IsActive));
    }
}