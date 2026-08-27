namespace CareerCopilot.Domain.Exceptions;

/// <summary>
/// Thrown when an operation would violate a domain rule.
/// </summary>
public sealed class DomainRuleException : Exception
{
    public DomainRuleException(string message)
        : base(message)
    {
    }
}