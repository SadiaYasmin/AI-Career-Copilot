using System.Security.Claims;
using CareerCopilot.Application.Common.Interfaces;

namespace CareerCopilot.API.Common;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var user = User;
            if (user is null || user.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var sub = user.FindFirst("sub")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
}