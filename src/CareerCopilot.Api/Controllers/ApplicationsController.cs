using CareerCopilot.API.Common;
using CareerCopilot.Application.Common.Models;
using CareerCopilot.Application.Features.Applications.Create;
using CareerCopilot.Application.Features.Applications.Delete;
using CareerCopilot.Application.Features.Applications.Dtos;
using CareerCopilot.Application.Features.Applications.Get;
using CareerCopilot.Application.Features.Applications.UpdateDetails;
using CareerCopilot.Application.Features.Applications.UpdateStatus;
using CareerCopilot.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/applications")]
public sealed class ApplicationsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ApplicationDto>>> List(
        int page = 1,
        int pageSize = 20,
        ApplicationStatus? status = null,
        CancellationToken ct = default)
        => Ok(new SuccessResponse<PagedResult<ApplicationDto>>(
            await Mediator.Send(new GetApplicationsQuery(page, pageSize, status), ct)));

    [HttpPost]
    public async Task<ActionResult<ApplicationDto>> Create(CreateApplicationCommand command, CancellationToken ct)
        => Ok(new SuccessResponse<ApplicationDto>(await Mediator.Send(command, ct)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApplicationDetailDto>> Get(Guid id, CancellationToken ct)
        => Ok(new SuccessResponse<ApplicationDetailDto>(await Mediator.Send(new GetApplicationQuery(id), ct)));

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ApplicationDetailDto>> UpdateStatus(Guid id, UpdateStatusRequest request, CancellationToken ct)
        => Ok(new SuccessResponse<ApplicationDetailDto>(
            await Mediator.Send(new UpdateApplicationStatusCommand(id, request.NewStatus), ct)));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApplicationDetailDto>> UpdateDetails(Guid id, UpdateDetailsRequest request, CancellationToken ct)
        => Ok(new SuccessResponse<ApplicationDetailDto>(
            await Mediator.Send(new UpdateApplicationDetailsCommand(id, request.Notes, request.FollowUpDate), ct)));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteApplicationCommand(id), ct);
        return Ok(new SuccessResponse<string>("Deleted."));
    }
}

public sealed record UpdateStatusRequest(ApplicationStatus NewStatus);

public sealed record UpdateDetailsRequest(string? Notes, DateTime? FollowUpDate);