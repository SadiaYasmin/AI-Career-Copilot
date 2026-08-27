namespace CareerCopilot.Application.Common.Interfaces;

/// <summary>
/// Resolves the authenticated user identity. Never trust client-supplied user ids.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}