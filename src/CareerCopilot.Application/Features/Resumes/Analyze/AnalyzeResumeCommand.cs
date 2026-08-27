using System.Text.Json;
using CareerCopilot.Application.Common.Ai;
using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Common.Services;
using CareerCopilot.Application.Features.Resumes.Dtos;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Resumes.Analyze;

public sealed record AnalyzeResumeCommand(Guid Id) : IRequest<ResumeAnalysisDto>;

public sealed class AnalyzeResumeCommandHandler : IRequestHandler<AnalyzeResumeCommand, ResumeAnalysisDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ICareerAiService _ai;
    private readonly ProfileSnapshotBuilder _personBuilder;

    public AnalyzeResumeCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        ICareerAiService ai,
        ProfileSnapshotBuilder personBuilder)
    {
        _db = db;
        _currentUser = currentUser;
        _ai = ai;
        _personBuilder = personBuilder;
    }

    public async Task<ResumeAnalysisDto> Handle(AnalyzeResumeCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var resume = await _db.Set<Resume>()
            .Where(r => r.Id == request.Id && r.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Resume not found.");

        if (resume.ParseFailed || string.IsNullOrWhiteSpace(resume.ParsedText))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["resume"] = new[]
                {
                    "We couldn't reliably extract text from this resume. Please upload a text-based PDF or enter your information manually."
                }
            });
        }

        var person = (await _personBuilder.BuildPersonAsync(userId, cancellationToken))
            ?? new AiPersonSnapshot(string.Empty, string.Empty, string.Empty,
                Array.Empty<AiSkill>(), Array.Empty<AiExperience>(), Array.Empty<AiProject>(),
                Array.Empty<AiEducation>(), Array.Empty<string>(), 0, string.Empty);

        var snapshot = new AiResumeSnapshot(
            resume.Id, resume.Name, resume.OriginalFileName, resume.ParsedText,
            resume.ParsedText.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList());

        var result = await _ai.AnalyzeResumeAsync(
            new ResumeAnalysisContext(snapshot, person), cancellationToken);

        var analysisPayload = new
        {
            score = result.Score,
            strengths = result.Strengths,
            improvements = result.Improvements,
            atRisk = result.AtRiskFindings,
            summary = result.Summary
        };

        resume.ResumeScore = result.Score;
        resume.ResumeAnalysisJson = JsonSerializer.Serialize(analysisPayload);
        resume.AnalyzedAt = DateTime.UtcNow;
        resume.UpdatedAt = DateTime.UtcNow;
        _db.Update(resume);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = new ResumeDto(resume.Id, resume.Name, resume.OriginalFileName, resume.FileType,
            resume.IsDefault, resume.UploadedAt, resume.ParseFailed, resume.ResumeScore, resume.AnalyzedAt);

        return new ResumeAnalysisDto(
            dto, result.Score, result.Strengths, result.Improvements,
            result.AtRiskFindings, result.Summary, result.UsedAi);
    }
}