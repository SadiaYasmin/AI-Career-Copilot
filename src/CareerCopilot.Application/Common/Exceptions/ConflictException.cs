namespace CareerCopilot.Application.Common.Exceptions;

public class ConflictException : Exception
{
    public string ErrorCode { get; } = "CONFLICT";

    public ConflictException(string message)
        : base(message)
    {
    }
}