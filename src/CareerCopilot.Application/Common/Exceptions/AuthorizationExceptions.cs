namespace CareerCopilot.Application.Common.Exceptions;

public class UnauthorizedException : Exception
{
    public string ErrorCode { get; } = "UNAUTHORIZED";

    public UnauthorizedException(string message)
        : base(message)
    {
    }
}

/// <summary>Raised when an AI operation is requested but no AI provider is configured.</summary>
public class AiUnavailableException : Exception
{
    public string ErrorCode { get; } = "AI_UNAVAILABLE";

    public AiUnavailableException(string message)
        : base(message)
    {
    }
}