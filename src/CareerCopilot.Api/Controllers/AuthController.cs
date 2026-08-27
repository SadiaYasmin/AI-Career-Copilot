using CareerCopilot.API.Common;
using CareerCopilot.Application.Features.Auth.Dtos;
using CareerCopilot.Application.Features.Auth.Login;
using CareerCopilot.Application.Features.Auth.Me;
using CareerCopilot.Application.Features.Auth.Register;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new RegisterCommand(request.Email, request.Password, request.FullName), ct);
        return Ok(Envelope(result));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new LoginCommand(request.Email, request.Password), ct);
        return Ok(Envelope(result));
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // JWT is stateless. The client discards the token; nothing to invalidate server-side.
        return Ok(Envelope(new { message = "Signed out." }));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var user = await Mediator.Send(new GetCurrentUserQuery(), ct);
        return Ok(Envelope(user));
    }

    private static SuccessResponse<T> Envelope<T>(T data) => new(data);
}
