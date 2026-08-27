using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Resumes.Dtos;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Resumes.SetDefault
{
    public sealed record SetDefaultResumeCommand(Guid Id) : IRequest<ResumeDto>;

    public sealed class SetDefaultResumeCommandHandler : IRequestHandler<SetDefaultResumeCommand, ResumeDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public SetDefaultResumeCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<ResumeDto> Handle(SetDefaultResumeCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var resume = await _db.Set<Resume>()
                .Where(r => r.Id == request.Id && r.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Resume not found.");

            var existing = await _db.Set<Resume>()
                .Where(r => r.UserId == userId && r.IsDefault && r.Id != resume.Id)
                .ToListAsync(cancellationToken);

            foreach (var r in existing)
            {
                r.ClearDefault();
                _db.Update(r);
            }

            resume.MarkAsDefault();
            _db.Update(resume);
            await _db.SaveChangesAsync(cancellationToken);

            return new ResumeDto(resume.Id, resume.Name, resume.OriginalFileName, resume.FileType,
                resume.IsDefault, resume.UploadedAt, resume.ParseFailed, resume.ResumeScore, resume.AnalyzedAt);
        }
    }
}

namespace CareerCopilot.Application.Features.Resumes.Delete
{
    public sealed record DeleteResumeCommand(Guid Id) : IRequest<Unit>;

    public sealed class DeleteResumeCommandHandler : IRequestHandler<DeleteResumeCommand, Unit>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IFileStorageService _fileStorage;

        public DeleteResumeCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IFileStorageService fileStorage)
        {
            _db = db;
            _currentUser = currentUser;
            _fileStorage = fileStorage;
        }

        public async Task<Unit> Handle(DeleteResumeCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var resume = await _db.Set<Resume>()
                .Where(r => r.Id == request.Id && r.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Resume not found.");

            _db.Remove(resume);

            var hasTailored = await _db.Set<TailoredResume>()
                .AnyAsync(t => t.ResumeId == request.Id && t.UserId == userId, cancellationToken);
            if (!hasTailored)
            {
                await _fileStorage.DeleteAsync(resume.StorageReference, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}