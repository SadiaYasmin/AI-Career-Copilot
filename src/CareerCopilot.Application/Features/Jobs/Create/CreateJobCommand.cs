using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Jobs.Dtos;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Jobs.Create
{
    public sealed class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, JobDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public CreateJobCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<JobDto> Handle(CreateJobCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var job = new Job
            {
                UserId = userId,
                Title = request.Title?.Trim() ?? string.Empty,
                CompanyName = request.CompanyName?.Trim() ?? string.Empty,
                Location = request.Location ?? string.Empty,
                EmploymentType = request.EmploymentType ?? string.Empty,
                Description = request.Description?.Trim() ?? string.Empty,
                SourceUrl = request.SourceUrl ?? string.Empty
            };

            _db.Add(job);
            await _db.SaveChangesAsync(cancellationToken);

            return new JobDto(job.Id, job.Title, job.CompanyName, job.Location, job.EmploymentType,
                job.SourceUrl, job.IsAnalyzed, job.AnalyzedAt, job.CreatedAt, null);
        }
    }
}

namespace CareerCopilot.Application.Features.Jobs.Update
{
    public sealed class UpdateJobCommandHandler : IRequestHandler<UpdateJobCommand, JobDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public UpdateJobCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<JobDto> Handle(UpdateJobCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var job = await _db.Set<Job>()
                .Where(j => j.Id == request.Id && j.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Job not found.");

            job.Title = request.Title?.Trim() ?? string.Empty;
            job.CompanyName = request.CompanyName?.Trim() ?? string.Empty;
            job.Location = request.Location ?? string.Empty;
            job.EmploymentType = request.EmploymentType ?? string.Empty;
            job.Description = request.Description?.Trim() ?? string.Empty;
            job.SourceUrl = request.SourceUrl ?? string.Empty;
            job.UpdatedAt = DateTime.UtcNow;

            _db.Update(job);
            await _db.SaveChangesAsync(cancellationToken);

            var latestMatch = await _db.Set<JobMatch>()
                .Where(m => m.JobId == job.Id && m.UserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => (int?)m.OverallScore)
                .FirstOrDefaultAsync(cancellationToken);

            return new JobDto(job.Id, job.Title, job.CompanyName, job.Location, job.EmploymentType,
                job.SourceUrl, job.IsAnalyzed, job.AnalyzedAt, job.CreatedAt, latestMatch);
        }
    }
}

namespace CareerCopilot.Application.Features.Jobs.Delete
{
    public sealed record DeleteJobCommand(Guid Id) : IRequest<MediatR.Unit>;

    public sealed class DeleteJobCommandHandler : IRequestHandler<DeleteJobCommand, MediatR.Unit>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public DeleteJobCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<MediatR.Unit> Handle(DeleteJobCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var job = await _db.Set<Job>()
                .Where(j => j.Id == request.Id && j.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Job not found.");

            _db.Remove(job);

            var matches = _db.Set<JobMatch>().Where(m => m.JobId == request.Id);
            _db.RemoveRange(matches);

            var skillGaps = _db.Set<SkillGap>().Where(s => s.JobId == request.Id);
            _db.RemoveRange(skillGaps);

            var requirements = _db.Set<JobRequirement>().Where(r => r.JobId == request.Id);
            _db.RemoveRange(requirements);

            await _db.SaveChangesAsync(cancellationToken);
            return MediatR.Unit.Value;
        }
    }
}