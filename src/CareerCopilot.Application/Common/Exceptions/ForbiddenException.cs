namespace CareerCopilot.Application.Common.Exceptions;

public class ForbiddenException : Exception
{
    public string ErrorCode { get; } = "FORBIDDEN";

    public ForbiddenException(string message)
        : base(message)
    {
    }
}