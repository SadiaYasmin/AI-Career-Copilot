using FluentValidation.Results;

namespace CareerCopilot.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public string ErrorCode { get; } = "VALIDATION_FAILED";

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this(failures
            .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray()))
    {
    }
}