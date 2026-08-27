using CareerCopilot.API.Common;
using CareerCopilot.Application.Features.RecruiterReadiness.Get;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/readiness")]
public sealed class ReadinessController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RecruiterReadinessDto>> Get(bool recalculate = false, CancellationToken ct = default)
        => Ok(new SuccessResponse<RecruiterReadinessDto>(
            await Mediator.Send(new GetRecruiterReadinessQuery(recalculate), ct)));
}