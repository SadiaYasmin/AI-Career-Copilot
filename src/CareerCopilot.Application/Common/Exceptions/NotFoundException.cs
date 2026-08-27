namespace CareerCopilot.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public string ErrorCode { get; } = "NOT_FOUND";

    public NotFoundException(string message)
        : base(message)
    {
    }
}