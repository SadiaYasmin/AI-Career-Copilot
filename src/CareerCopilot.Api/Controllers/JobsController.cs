using CareerCopilot.API.Common;
using CareerCopilot.Application.Common.Models;
using CareerCopilot.Application.Features.JobMatching.Calculate;
using CareerCopilot.Application.Features.JobMatching.Dtos;
using CareerCopilot.Application.Features.JobMatching.Get;
using CareerCopilot.Application.Features.Jobs.Analyze;
using CareerCopilot.Application.Features.Jobs.Create;
using CareerCopilot.Application.Features.Jobs.Delete;
using CareerCopilot.Application.Features.Jobs.Dtos;
using CareerCopilot.Application.Features.Jobs.Get;
using CareerCopilot.Application.Features.SkillGaps.Get;
using CareerCopilot.Application.Features.Tailoring.CoverLetter;
using CareerCopilot.Application.Features.Tailoring.Dtos;
using CareerCopilot.Application.Features.Tailoring.Tailor;
using CareerCopilot.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/jobs")]
public sealed class JobsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<JobDto>>> List(int page = 1, int pageSize = 20, CancellationToken ct = default)
        => Ok(new SuccessResponse<PagedResult<JobDto>>(await Mediator.Send(new GetJobsQuery(page, pageSize), ct)));

    [HttpPost]
    public async Task<ActionResult<JobDto>> Create(CreateJobCommand command, CancellationToken ct)
        => Ok(new SuccessResponse<JobDto>(await Mediator.Send(command, ct)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobDetailDto>> Get(Guid id, CancellationToken ct)
        => Ok(new SuccessResponse<JobDetailDto>(await Mediator.Send(new GetJobDetailsQuery(id), ct)));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobDto>> Update(Guid id, UpdateJobCommand command, CancellationToken ct)
        => Ok(new SuccessResponse<JobDto>(await Mediator.Send(command with { Id = id }, ct)));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteJobCommand(id), ct);
        return Ok(new SuccessResponse<string>("Deleted."));
    }

    [HttpPost("{id:guid}/analyze")]
    public async Task<ActionResult<JobDetailDto>> Analyze(Guid id, CancellationToken ct)
        => Ok(new SuccessResponse<JobDetailDto>(await Mediator.Send(new AnalyzeJobCommand(id), ct)));

    [HttpPost("{id:guid}/match")]
    public async Task<ActionResult<JobMatchDto>> CalculateMatch(Guid id, Guid? resumeId = null, CancellationToken ct = default)
        => Ok(new SuccessResponse<JobMatchDto>(await Mediator.Send(new CalculateJobMatchCommand(id, resumeId), ct)));

    [HttpGet("{id:guid}/match")]
    public async Task<ActionResult<JobMatchDto>> GetMatch(Guid id, CancellationToken ct)
        => Ok(new SuccessResponse<JobMatchDto>(await Mediator.Send(new GetJobMatchQuery(id), ct)));

    [HttpGet("{id:guid}/skill-gaps")]
    public async Task<ActionResult<IReadOnlyList<SkillGapDto>>> SkillGaps(Guid id, CancellationToken ct)
        => Ok(new SuccessResponse<IReadOnlyList<SkillGapDto>>(await Mediator.Send(new GetSkillGapsQuery(id), ct)));

    [HttpPost("{id:guid}/tailor-resume")]
    public async Task<ActionResult<TailoredResumeDetailDto>> TailorResume(
        Guid id,
        [FromBody] TailorRequest request,
        CancellationToken ct)
        => Ok(new SuccessResponse<TailoredResumeDetailDto>(await Mediator.Send(
            new GenerateTailoredResumeCommand(request.ResumeId, id, request.Mode), ct)));

    [HttpPost("{id:guid}/cover-letter")]
    public async Task<ActionResult<CoverLetterDto>> CoverLetter(
        Guid id,
        [FromBody] CoverLetterRequest request,
        CancellationToken ct)
        => Ok(new SuccessResponse<CoverLetterDto>(await Mediator.Send(
            new GenerateCoverLetterCommand(id, request.Length ?? "Standard", request.Tone ?? "Professional"), ct)));
}

public sealed record TailorRequest(Guid ResumeId, TailoringMode Mode);

public sealed record CoverLetterRequest(string? Length, string? Tone);