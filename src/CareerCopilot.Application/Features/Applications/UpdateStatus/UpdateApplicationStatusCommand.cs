using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Applications.Dtos;
using CareerCopilot.Application.Features.Applications.Shared;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = CareerCopilot.Domain.Entities.Application;

namespace CareerCopilot.Application.Features.Applications.UpdateStatus
{
    public sealed class UpdateApplicationStatusCommandHandler : IRequestHandler<UpdateApplicationStatusCommand, ApplicationDetailDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public UpdateApplicationStatusCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<ApplicationDetailDto> Handle(UpdateApplicationStatusCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var application = await _db.Set<ApplicationEntity>()
                .Where(a => a.Id == request.Id && a.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Application not found.");

            application.UpdateStatus(request.NewStatus, DateTime.UtcNow);
            _db.Update(application);
            await _db.SaveChangesAsync(cancellationToken);

            return await ApplicationMapper.ToDetailDtoAsync(_db, application, userId, cancellationToken);
        }
    }
}

namespace CareerCopilot.Application.Features.Applications.UpdateDetails
{
    public sealed class UpdateApplicationDetailsCommandHandler : IRequestHandler<UpdateApplicationDetailsCommand, ApplicationDetailDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public UpdateApplicationDetailsCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<ApplicationDetailDto> Handle(UpdateApplicationDetailsCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var application = await _db.Set<ApplicationEntity>()
                .Where(a => a.Id == request.Id && a.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Application not found.");

            application.Notes = request.Notes;
            application.FollowUpDate = request.FollowUpDate;
            application.UpdatedAt = DateTime.UtcNow;

            _db.Update(application);
            await _db.SaveChangesAsync(cancellationToken);

            return await ApplicationMapper.ToDetailDtoAsync(_db, application, userId, cancellationToken);
        }
    }
}

namespace CareerCopilot.Application.Features.Applications.Delete
{
    public sealed record DeleteApplicationCommand(Guid Id) : IRequest<MediatR.Unit>;

    public sealed class DeleteApplicationCommandHandler : IRequestHandler<DeleteApplicationCommand, MediatR.Unit>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public DeleteApplicationCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<MediatR.Unit> Handle(DeleteApplicationCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var application = await _db.Set<ApplicationEntity>()
                .Where(a => a.Id == request.Id && a.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Application not found.");

            _db.Remove(application);
            await _db.SaveChangesAsync(cancellationToken);
            return MediatR.Unit.Value;
        }
    }
}