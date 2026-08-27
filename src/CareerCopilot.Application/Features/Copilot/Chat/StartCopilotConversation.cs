using CareerCopilot.Application.Common.Ai;
using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Common.Services;
using CareerCopilot.Application.Features.Copilot.Dtos;
using CareerCopilot.Domain.Entities;
using CareerCopilot.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ApplicationEntity = CareerCopilot.Domain.Entities.Application;

namespace CareerCopilot.Application.Features.Copilot.Chat;

public sealed class StartCopilotConversationCommandHandler : IRequestHandler<StartCopilotConversationCommand, CopilotReplyDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ProfileSnapshotBuilder _personSnapshot;
    private readonly JobSnapshotBuilder _jobSnapshot;
    private readonly ICareerAiService _ai;

    public StartCopilotConversationCommandHandler(
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

    public async Task<CopilotReplyDto> Handle(StartCopilotConversationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["message"] = new[] { "Message cannot be empty." }
            });
        }

        var conversation = new CopilotConversation
        {
            UserId = userId,
            JobId = request.JobId,
            Title = BuildTitle(request.Message)
        };
        _db.Add(conversation);

        var userMessage = new CopilotMessage
        {
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = request.Message.Trim(),
            ContextType = "chat"
        };
        _db.Add(userMessage);

        var reply = await GenerateReplyAsync(userId, conversation, request.Message, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new CopilotReplyDto(conversation.Id,
            new CopilotMessageDto(reply.Id, reply.Role.ToString(), reply.Content, reply.CreatedAt));
    }

    private async Task<CopilotMessage> GenerateReplyAsync(
        Guid userId,
        CopilotConversation conversation,
        string userMessage,
        CancellationToken ct)
    {
        var person = await _personSnapshot.BuildPersonAsync(userId, ct)
            ?? new AiPersonSnapshot(string.Empty, string.Empty, string.Empty,
                Array.Empty<AiSkill>(), Array.Empty<AiExperience>(), Array.Empty<AiProject>(),
                Array.Empty<AiEducation>(), Array.Empty<string>(), 0, string.Empty);

        AiResumeSnapshot? resume = null;
        var defaultResume = await _db.Set<Resume>()
            .Where(r => r.UserId == userId && r.IsDefault)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (defaultResume is not null)
        {
            resume = await _personSnapshot.BuildResumeAsync(defaultResume.Id, userId, ct);
        }

        AiJobSnapshot? job = null;
        if (conversation.JobId is not null)
        {
            job = await _jobSnapshot.BuildAsync(conversation.JobId.Value, userId, ct);
        }

        var supporting = await BuildSupportingDataAsync(_db, userId, ct, job);

        var recent = await _db.Set<CopilotMessage>()
            .Where(m => m.ConversationId == conversation.Id)
            .OrderByDescending(m => m.CreatedAt)
            .Take(6)
            .Select(m => m.Content)
            .ToListAsync(ct);
        recent.Reverse();

        var context = new CopilotContext(
            userMessage,
            conversation.Title ?? "Career Copilot chat",
            person,
            resume,
            job,
            recent,
            supporting);

        var replyText = await _ai.GenerateCopilotReplyAsync(context, ct);

        var reply = new CopilotMessage
        {
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = replyText ?? "I'm not able to answer that right now. Please try again.",
            ContextType = "chat"
        };
        _db.Add(reply);
        return reply;
    }

    internal static async Task<IReadOnlyDictionary<string, string>> BuildSupportingDataAsync(
        IApplicationDbContext db,
        Guid userId,
        CancellationToken ct,
        AiJobSnapshot? job)
    {
        var data = new Dictionary<string, string>();

        var latestMatch = await db.Set<JobMatch>()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => (int?)m.OverallScore)
            .FirstOrDefaultAsync(ct);

        if (latestMatch is not null)
        {
            data["latest_match_score"] = latestMatch.Value.ToString();
        }

        var applicationsCount = await db.Set<ApplicationEntity>()
            .CountAsync(a => a.UserId == userId, ct);
        data["applications_count"] = applicationsCount.ToString();

        var gaps = await db.Set<SkillGap>()
            .Where(s => s.UserId == userId && s.Priority == SkillPriority.Critical)
            .Select(s => s.SkillName)
            .Take(5)
            .ToListAsync(ct);

        if (gaps.Count > 0)
        {
            data["critical_skill_gaps"] = string.Join(", ", gaps);
        }

        return data;
    }

    private static string BuildTitle(string message)
    {
        var cleaned = string.Join(' ', message.Trim().Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        return cleaned.Length <= 48 ? cleaned : cleaned[..48] + "...";
    }
}

public sealed class SendCopilotMessageCommandHandler : IRequestHandler<SendCopilotMessageCommand, CopilotReplyDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ProfileSnapshotBuilder _personSnapshot;
    private readonly JobSnapshotBuilder _jobSnapshot;
    private readonly ICareerAiService _ai;

    public SendCopilotMessageCommandHandler(
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

    public async Task<CopilotReplyDto> Handle(SendCopilotMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["message"] = new[] { "Message cannot be empty." }
            });
        }

        var conversation = await _db.Set<CopilotConversation>()
            .Where(c => c.Id == request.ConversationId && c.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Conversation not found.");

        var userMessage = new CopilotMessage
        {
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = request.Message.Trim(),
            ContextType = "chat"
        };
        _db.Add(userMessage);

        var reply = await GenerateReplyAsync(userId, conversation, request.Message, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new CopilotReplyDto(conversation.Id,
            new CopilotMessageDto(reply.Id, reply.Role.ToString(), reply.Content, reply.CreatedAt));
    }

    private async Task<CopilotMessage> GenerateReplyAsync(
        Guid userId,
        CopilotConversation conversation,
        string userMessage,
        CancellationToken ct)
    {
        var person = await _personSnapshot.BuildPersonAsync(userId, ct)
            ?? new AiPersonSnapshot(string.Empty, string.Empty, string.Empty,
                Array.Empty<AiSkill>(), Array.Empty<AiExperience>(), Array.Empty<AiProject>(),
                Array.Empty<AiEducation>(), Array.Empty<string>(), 0, string.Empty);

        AiResumeSnapshot? resume = null;
        var defaultResume = await _db.Set<Resume>()
            .Where(r => r.UserId == userId && r.IsDefault)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (defaultResume is not null)
        {
            resume = await _personSnapshot.BuildResumeAsync(defaultResume.Id, userId, ct);
        }

        AiJobSnapshot? job = null;
        if (conversation.JobId is not null)
        {
            job = await _jobSnapshot.BuildAsync(conversation.JobId.Value, userId, ct);
        }

        var recent = await _db.Set<CopilotMessage>()
            .Where(m => m.ConversationId == conversation.Id)
            .OrderByDescending(m => m.CreatedAt)
            .Take(6)
            .Select(m => m.Content)
            .ToListAsync(ct);
        recent.Reverse();

        var context = new CopilotContext(
            userMessage,
            conversation.Title ?? "Career Copilot chat",
            person,
            resume,
            job,
            recent,
            new Dictionary<string, string>());

        var replyText = await _ai.GenerateCopilotReplyAsync(context, ct);

        var reply = new CopilotMessage
        {
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = replyText ?? "I'm not able to answer that right now. Please try again.",
            ContextType = "chat"
        };
        _db.Add(reply);
        return reply;
    }
}