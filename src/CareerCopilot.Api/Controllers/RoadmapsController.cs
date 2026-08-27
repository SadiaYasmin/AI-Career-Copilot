using CareerCopilot.API.Common;
using CareerCopilot.Application.Features.CareerRoadmaps.Dtos;
using CareerCopilot.Application.Features.CareerRoadmaps.Generate;
using CareerCopilot.Application.Features.CareerRoadmaps.Get;
using CareerCopilot.Application.Features.CareerRoadmaps.UpdateStatus;
using CareerCopilot.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/roadmaps")]
public sealed class RoadmapsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RoadmapDto>> Get(Guid? id = null, CancellationToken ct = default)
        => Ok(new SuccessResponse<RoadmapDto>(await Mediator.Send(new GetCareerRoadmapQuery(id), ct)));

    [HttpPost]
    public async Task<ActionResult<RoadmapDto>> Generate(GenerateRoadmapRequest request, CancellationToken ct)
        => Ok(new SuccessResponse<RoadmapDto>(
            await Mediator.Send(new GenerateCareerRoadmapCommand(request.TargetRole), ct)));

    [HttpPut("tasks/{id:guid}")]
    public async Task<ActionResult<RoadmapTaskDto>> UpdateTaskStatus(Guid id, UpdateTaskRequest request, CancellationToken ct)
        => Ok(new SuccessResponse<RoadmapTaskDto>(
            await Mediator.Send(new UpdateRoadmapTaskStatusCommand(id, request.NewStatus), ct)));
}

public sealed record GenerateRoadmapRequest(string? TargetRole);

public sealed record UpdateTaskRequest(RoadmapTaskStatus NewStatus);