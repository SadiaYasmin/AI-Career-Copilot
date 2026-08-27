using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Auth.Dtos;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Auth.Me;

public sealed record GetCurrentUserQuery : IRequest<UserDto>;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var user = await _db.Set<User>()
            .Where(u => u.Id == userId)
            .Select(u => new UserDto(u.Id, u.Email, u.Role.ToString(), u.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

        return user ?? throw new NotFoundException("User not found.");
    }
}