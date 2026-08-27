using CareerCopilot.API.Common;
using CareerCopilot.Application.Features.Dashboard.Get;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken ct)
        => Ok(new SuccessResponse<DashboardDto>(await Mediator.Send(new GetDashboardQuery(), ct)));
}