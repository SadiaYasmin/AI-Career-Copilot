using CareerCopilot.Application.Common.Ai;
using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Common.Services;
using CareerCopilot.Application.Features.Tailoring.Dtos;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CoverLetterEntity = CareerCopilot.Domain.Entities.CoverLetter;

namespace CareerCopilot.Application.Features.Tailoring.CoverLetter
{

public sealed class GenerateCoverLetterCommandHandler : IRequestHandler<GenerateCoverLetterCommand, CoverLetterDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ProfileSnapshotBuilder _personSnapshot;
    private readonly JobSnapshotBuilder _jobSnapshot;
    private readonly ICareerAiService _ai;

    public GenerateCoverLetterCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        ProfileSnapshotBuilder personSnapshot,
        JobSnapshotBuilder jobSnapshot,
        ICareerAiService ai)
    {
        _db = db;
        _currentUser = currentUser;
        _personSnapshot = personSnapshot;
        _jobSnapshot = jobSnapshot;
        _ai = ai;
    }

    public async Task<CoverLetterDto> Handle(GenerateCoverLetterCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var job = await _jobSnapshot.BuildAsync(request.JobId, userId, cancellationToken)
            ?? throw new NotFoundException("Job not found.");

        var person = await _personSnapshot.BuildPersonAsync(userId, cancellationToken)
            ?? new AiPersonSnapshot(string.Empty, string.Empty, string.Empty,
                Array.Empty<AiSkill>(), Array.Empty<AiExperience>(), Array.Empty<AiProject>(),
                Array.Empty<AiEducation>(), Array.Empty<string>(), 0, string.Empty);

        var context = new CoverLetterContext(person, job, request.Length ?? "Standard", request.Tone ?? "Professional");
        var result = await _ai.GenerateCoverLetterAsync(context, cancellationToken);

        var letter = new CoverLetterEntity
        {
            UserId = userId,
            JobId = request.JobId,
            Content = result.Content,
            Length = request.Length ?? "Standard",
            Tone = request.Tone ?? "Professional"
        };
        _db.Add(letter);
        await _db.SaveChangesAsync(cancellationToken);

        return new CoverLetterDto(
            letter.Id, letter.JobId, job.Title, job.Company,
            letter.Content, letter.Length, letter.Tone, letter.CreatedAt);
    }
}

}

namespace CareerCopilot.Application.Features.Tailoring.Get
{
    public sealed record GetCoverLettersQuery(Guid? JobId = null) : IRequest<IReadOnlyList<CoverLetterDto>>;

    public sealed class GetCoverLettersQueryHandler : IRequestHandler<GetCoverLettersQuery, IReadOnlyList<CoverLetterDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public GetCoverLettersQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<CoverLetterDto>> Handle(GetCoverLettersQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var query = _db.Set<CoverLetterEntity>().Where(c => c.UserId == userId);

            if (request.JobId is not null)
            {
                query = query.Where(c => c.JobId == request.JobId);
            }

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(cancellationToken);

            var jobIds = items.Select(c => c.JobId).Distinct().ToList();
            var jobs = await _db.Set<Job>()
                .Where(j => jobIds.Contains(j.Id))
                .ToDictionaryAsync(j => j.Id, j => new { j.Title, j.CompanyName }, cancellationToken);

            return items.Select(c =>
            {
                jobs.TryGetValue(c.JobId, out var job);
                return new CoverLetterDto(
                    c.Id, c.JobId,
                    job?.Title ?? "Unknown", job?.CompanyName ?? string.Empty,
                    c.Content, c.Length, c.Tone, c.CreatedAt);
            }).ToList();
        }
    }
}