using CareerCopilot.API.Common;
using CareerCopilot.Application.Features.Tailoring.Dtos;
using CareerCopilot.Application.Features.Tailoring.Get;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/tailored")]
public sealed class TailoringController : ApiControllerBase
{
    [HttpGet("resumes")]
    public async Task<ActionResult<IReadOnlyList<TailoredResumeDto>>> ListTailored(Guid? resumeId = null, CancellationToken ct = default)
        => Ok(new SuccessResponse<IReadOnlyList<TailoredResumeDto>>(
            await Mediator.Send(new GetTailoredResumesQuery(resumeId), ct)));

    [HttpGet("resumes/{id:guid}")]
    public async Task<ActionResult<TailoredResumeDetailDto>> GetTailored(Guid id, CancellationToken ct)
        => Ok(new SuccessResponse<TailoredResumeDetailDto>(await Mediator.Send(new GetTailoredResumeQuery(id), ct)));

    [HttpGet("cover-letters")]
    public async Task<ActionResult<IReadOnlyList<CoverLetterDto>>> ListCoverLetters(Guid? jobId = null, CancellationToken ct = default)
        => Ok(new SuccessResponse<IReadOnlyList<CoverLetterDto>>(
            await Mediator.Send(new GetCoverLettersQuery(jobId), ct)));
}