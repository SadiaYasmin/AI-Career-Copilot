using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Auth.Dtos;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IApplicationDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException("This account has been deactivated.");
        }

        var token = _jwtTokenService.CreateToken(user.Id, user.Email, user.Role);

        return new AuthResponse(token, new UserDto(user.Id, user.Email, user.Role.ToString(), user.IsActive));
    }
}