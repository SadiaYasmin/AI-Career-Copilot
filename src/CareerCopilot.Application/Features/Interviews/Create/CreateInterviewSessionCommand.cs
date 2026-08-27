using CareerCopilot.Application.Common.Ai;
using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Common.Services;
using CareerCopilot.Application.Features.Interviews.Dtos;
using CareerCopilot.Application.Features.Interviews.Shared;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Interviews.Create;

public sealed class CreateInterviewSessionCommandHandler : IRequestHandler<CreateInterviewSessionCommand, InterviewSessionDetailDto>
{
    private const int QuestionCount = 5;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly JobSnapshotBuilder _jobSnapshot;
    private readonly ProfileSnapshotBuilder _personSnapshot;
    private readonly ICareerAiService _ai;

    public CreateInterviewSessionCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        JobSnapshotBuilder jobSnapshot,
        ProfileSnapshotBuilder personSnapshot,
        ICareerAiService ai)
    {
        _db = db;
        _currentUser = currentUser;
        _jobSnapshot = jobSnapshot;
        _personSnapshot = personSnapshot;
        _ai = ai;
    }

    public async Task<InterviewSessionDetailDto> Handle(CreateInterviewSessionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var job = await _jobSnapshot.BuildAsync(request.JobId, userId, cancellationToken)
            ?? throw new NotFoundException("Job not found.");

        var person = await _personSnapshot.BuildPersonAsync(userId, cancellationToken)
            ?? new AiPersonSnapshot(string.Empty, string.Empty, string.Empty,
                Array.Empty<AiSkill>(), Array.Empty<AiExperience>(), Array.Empty<AiProject>(),
                Array.Empty<AiEducation>(), Array.Empty<string>(), 0, string.Empty);

        var context = new InterviewContext(
            job, person, null,
            request.Mode.ToString(),
            job.Requirements.Select(r => r.Name).ToList(),
            job.Requirements.Where(r => r.RequirementType == "Required").Select(r => r.Name).ToList());

        var result = await _ai.GenerateInterviewQuestionsAsync(context, cancellationToken);

        var questions = result.Questions.Take(QuestionCount).ToList();

        var session = new InterviewSession
        {
            UserId = userId,
            JobId = request.JobId,
            Mode = request.Mode
        };
        _db.Add(session);

        var order = 1;
        foreach (var q in questions)
        {
            _db.Add(new InterviewQuestion
            {
                InterviewSessionId = session.Id,
                Question = q.Question,
                QuestionType = InterviewMapper.MapQuestionType(q.QuestionType),
                Order = order++
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await InterviewMapper.ToDetailDtoAsync(_db, session, userId, cancellationToken);
    }
}