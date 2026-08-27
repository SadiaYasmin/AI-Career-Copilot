using CareerCopilot.Application.Common.Ai;
using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Common.Services;
using CareerCopilot.Application.Features.CareerRoadmaps.Dtos;
using CareerCopilot.Domain.Entities;
using CareerCopilot.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.CareerRoadmaps.Generate
{

public sealed class GenerateCareerRoadmapCommandHandler : IRequestHandler<GenerateCareerRoadmapCommand, RoadmapDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ProfileSnapshotBuilder _personSnapshot;
    private readonly ICareerAiService _ai;

    public GenerateCareerRoadmapCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        ProfileSnapshotBuilder personSnapshot,
        ICareerAiService ai)
    {
        _db = db;
        _currentUser = currentUser;
        _personSnapshot = personSnapshot;
        _ai = ai;
    }

    public async Task<RoadmapDto> Handle(GenerateCareerRoadmapCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var person = await _personSnapshot.BuildPersonAsync(userId, cancellationToken)
            ?? throw new ValidationException(new Dictionary<string, string[]>
            {
                ["profile"] = new[] { "Complete your career profile before generating a roadmap." }
            });

        var targetRole = string.IsNullOrWhiteSpace(request.TargetRole) ? person.TargetRole : request.TargetRole.Trim();

        if (string.IsNullOrWhiteSpace(targetRole))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["targetRole"] = new[] { "Tell us your target role or set one in your career profile." }
            });
        }

        var recentGaps = await _db.Set<SkillGap>()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .Take(8)
            .Select(s => s.SkillName + " (" + s.Priority + ")")
            .ToListAsync(cancellationToken);

        var roadmapContext = new CareerRoadmapContext(person, targetRole, recentGaps, Array.Empty<string>());
        var result = await _ai.GenerateCareerRoadmapAsync(roadmapContext, cancellationToken);

        var oldRoadmaps = _db.Set<CareerRoadmap>().Where(r => r.UserId == userId);
        var oldTasks = _db.Set<RoadmapTask>().Where(t => oldRoadmaps.Select(r => r.Id).Contains(t.CareerRoadmapId));
        _db.RemoveRange(oldTasks);
        _db.RemoveRange(oldRoadmaps);

        var roadmap = new CareerRoadmap
        {
            UserId = userId,
            TargetRole = result.TargetRole,
            Description = result.Description
        };
        _db.Add(roadmap);

        foreach (var task in result.Tasks)
        {
            var priority = MapPriority(task.Priority);

            _db.Add(new RoadmapTask
            {
                CareerRoadmapId = roadmap.Id,
                Title = task.Title,
                Description = task.Description,
                Skill = task.Skill,
                Priority = priority,
                Status = RoadmapTaskStatus.Pending,
                DueDate = MapDueDate(task.Month)
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await ToDtoAsync(_db, roadmap, cancellationToken);
    }

    internal static async Task<RoadmapDto> ToDtoAsync(IApplicationDbContext db, CareerRoadmap roadmap, CancellationToken ct)
    {
        var tasks = await db.Set<RoadmapTask>()
            .Where(t => t.CareerRoadmapId == roadmap.Id)
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Title)
            .Select(t => new RoadmapTaskDto(
                t.Id, t.Title, t.Description, string.Empty, t.Skill,
                t.Priority.ToString(), t.Status.ToString(), t.DueDate))
            .ToListAsync(ct);

        return new RoadmapDto(roadmap.Id, roadmap.TargetRole, roadmap.Description, roadmap.CreatedAt, tasks);
    }

    private static SkillPriority MapPriority(string priority)
        => priority?.ToLowerInvariant() switch
        {
            "high" or "critical" => SkillPriority.High,
            "low" => SkillPriority.Low,
            _ => SkillPriority.Medium
        };

    private static DateTime? MapDueDate(string month)
    {
        if (int.TryParse(System.Text.RegularExpressions.Regex.Match(month ?? string.Empty, @"\d+").Value, out var months))
        {
            months = Math.Clamp(months, 1, 12);
            return DateTime.UtcNow.AddMonths(months - 1).Date;
        }

        return month?.ToLowerInvariant() switch
        {
            "month 1" or "now" => DateTime.UtcNow.Date,
            "immediate" or "urgent" => DateTime.UtcNow.Date,
            "this week" => DateTime.UtcNow.Date.AddDays(7),
            "this month" => DateTime.UtcNow.Date.AddDays(30),
            _ => null
        };
    }
}

}

namespace CareerCopilot.Application.Features.CareerRoadmaps.Get
{
    public sealed record GetCareerRoadmapQuery(Guid? Id = null) : IRequest<RoadmapDto>;

    public sealed class GetCareerRoadmapQueryHandler : IRequestHandler<GetCareerRoadmapQuery, RoadmapDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public GetCareerRoadmapQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<RoadmapDto> Handle(GetCareerRoadmapQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            CareerRoadmap? roadmap;
            if (request.Id is not null)
            {
                roadmap = await _db.Set<CareerRoadmap>()
                    .Where(r => r.Id == request.Id && r.UserId == userId)
                    .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new NotFoundException("Career roadmap not found.");
            }
            else
            {
                roadmap = await _db.Set<CareerRoadmap>()
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (roadmap is null)
                {
                    throw new NotFoundException("No career roadmap yet. Generate one from your profile.");
                }
            }

            return await Generate.GenerateCareerRoadmapCommandHandler.ToDtoAsync(_db, roadmap, cancellationToken);
        }
    }
}