using CareerCopilot.Domain.Enums;

namespace CareerCopilot.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string email, UserRole role);
}