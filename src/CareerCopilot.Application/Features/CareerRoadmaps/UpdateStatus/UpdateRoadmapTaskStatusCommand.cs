using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.CareerRoadmaps.Dtos;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.CareerRoadmaps.UpdateStatus;

public sealed class UpdateRoadmapTaskStatusCommandHandler : IRequestHandler<UpdateRoadmapTaskStatusCommand, RoadmapTaskDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateRoadmapTaskStatusCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<RoadmapTaskDto> Handle(UpdateRoadmapTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var task = await _db.Set<RoadmapTask>()
            .Where(t => t.Id == request.TaskId && t.CareerRoadmap!.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Roadmap task not found.");

        task.Status = request.NewStatus;
        task.UpdatedAt = DateTime.UtcNow;
        _db.Update(task);
        await _db.SaveChangesAsync(cancellationToken);

        return new RoadmapTaskDto(task.Id, task.Title, task.Description, string.Empty, task.Skill,
            task.Priority.ToString(), task.Status.ToString(), task.DueDate);
    }
}