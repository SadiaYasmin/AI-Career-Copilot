using CareerCopilot.Domain.Enums;

namespace CareerCopilot.Application.Features.Auth.Dtos;

public sealed record RegisterRequest(string Email, string Password, string FullName);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserDto(Guid Id, string Email, string Role, bool IsActive);

public sealed record AuthResponse(string Token, UserDto User);