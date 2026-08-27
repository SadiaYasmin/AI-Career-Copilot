using CareerCopilot.API.Common;
using CareerCopilot.Application.Features.Profiles.Dtos;
using CareerCopilot.Application.Features.Profiles.Get;
using CareerCopilot.Application.Features.Profiles.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get(CancellationToken ct)
        => Ok(new SuccessResponse<ProfileDto>(await Mediator.Send(new GetProfileQuery(), ct)));

    [HttpPut]
    public async Task<ActionResult<ProfileDto>> Update(UpdateProfileCommand command, CancellationToken ct)
        => Ok(new SuccessResponse<ProfileDto>(await Mediator.Send(command, ct)));
}