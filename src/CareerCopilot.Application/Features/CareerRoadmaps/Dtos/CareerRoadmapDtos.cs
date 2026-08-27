using CareerCopilot.Domain.Enums;

namespace CareerCopilot.Application.Features.CareerRoadmaps.Dtos;

public sealed record RoadmapTaskDto(
    Guid Id,
    string Title,
    string Description,
    string Month,
    string Skill,
    string Priority,
    string Status,
    DateTime? DueDate);

public sealed record RoadmapDto(
    Guid Id,
    string TargetRole,
    string Description,
    DateTime CreatedAt,
    IReadOnlyList<RoadmapTaskDto> Tasks);

public sealed record GenerateCareerRoadmapCommand(string? TargetRole) : MediatR.IRequest<RoadmapDto>;

public sealed record UpdateRoadmapTaskStatusCommand(Guid TaskId, RoadmapTaskStatus NewStatus) : MediatR.IRequest<RoadmapTaskDto>;