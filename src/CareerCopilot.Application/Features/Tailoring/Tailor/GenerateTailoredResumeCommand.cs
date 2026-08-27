using CareerCopilot.Application.Common.Ai;
using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Common.Services;
using CareerCopilot.Application.Features.Tailoring.Dtos;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Tailoring.Tailor;

public sealed class GenerateTailoredResumeCommandHandler
    : IRequestHandler<GenerateTailoredResumeCommand, TailoredResumeDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ProfileSnapshotBuilder _personSnapshot;
    private readonly JobSnapshotBuilder _jobSnapshot;
    private readonly ICareerAiService _ai;

    public GenerateTailoredResumeCommandHandler(
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

    public async Task<TailoredResumeDetailDto> Handle(GenerateTailoredResumeCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var resume = await _personSnapshot.BuildResumeAsync(request.ResumeId, userId, cancellationToken)
            ?? throw new NotFoundException("Resume not found.");

        var job = await _jobSnapshot.BuildAsync(request.JobId, userId, cancellationToken)
            ?? throw new NotFoundException("Job not found.");

        if (job.Requirements.Count == 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["jobId"] = new[] { "Analyze the job description before tailoring a resume to it." }
            });
        }

        if (string.IsNullOrWhiteSpace(resume.ParsedText))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["resumeId"] = new[] { "This resume has no extractable text. Upload a text-based PDF." }
            });
        }

        var latestMatch = await _db.Set<JobMatch>()
            .Where(m => m.JobId == request.JobId && m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var context = new TailorResumeContext(
            resume,
            job,
            request.Mode.ToString(),
            latestMatch is null ? Array.Empty<string>() : (IReadOnlyList<string>)ExtractStatuses(latestMatch, "Strong"),
            latestMatch is null ? Array.Empty<string>() : (IReadOnlyList<string>)ExtractStatuses(latestMatch, "Missing"));

        var result = await _ai.TailorResumeAsync(context, cancellationToken);

        var tailored = new TailoredResume
        {
            UserId = userId,
            ResumeId = request.ResumeId,
            JobId = request.JobId,
            Mode = request.Mode,
            Content = result.Content,
            OriginalContent = resume.ParsedText,
            ChangesSummary = result.ChangesSummary
        };
        _db.Add(tailored);
        await _db.SaveChangesAsync(cancellationToken);

        return new TailoredResumeDetailDto(
            tailored.Id,
            tailored.ResumeId,
            tailored.JobId,
            job.Title,
            job.Company,
            tailored.Mode.ToString(),
            tailored.Content,
            tailored.OriginalContent,
            tailored.Separator,
            tailored.ChangesSummary,
            tailored.CreatedAt);
    }

    private static List<string> ExtractStatuses(JobMatch match, string status)
        => match.EvidenceJson is null
            ? new List<string>()
            : (System.Text.Json.JsonSerializer.Deserialize<List<EvidenceItem>>(match.EvidenceJson)
                ?? new List<EvidenceItem>())
                .Where(e => string.Equals(e.Status, status, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Name ?? string.Empty)
                .Where(n => n.Length > 0)
                .ToList();

    private sealed class EvidenceItem
    {
        public string? Name { get; set; }
        public string? Status { get; set; }
    }
}