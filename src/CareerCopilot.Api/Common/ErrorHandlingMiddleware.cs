using System.Text.Json;
using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Domain.Exceptions;

namespace CareerCopilot.API.Common;

public sealed class ApiError
{
    public bool Success { get; } = false;
    public string Message { get; init; } = string.Empty;
    public string ErrorCode { get; init; } = "INTERNAL_ERROR";
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}

public sealed class ErrorHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (status, error) = ToError(exception);

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception in {Path}", context.Request.Path);
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(error, JsonOptions));
    }

    private static (int Status, ApiError Error) ToError(Exception exception)
        => exception switch
        {
            ValidationException ex => (StatusCodes.Status400BadRequest,
                new ApiError { Message = ex.Message, ErrorCode = ex.ErrorCode, Errors = ex.Errors }),
            DomainRuleException ex => (StatusCodes.Status400BadRequest,
                new ApiError { Message = ex.Message, ErrorCode = "DOMAIN_RULE_VIOLATION" }),
            UnauthorizedException ex => (StatusCodes.Status401Unauthorized,
                new ApiError { Message = ex.Message, ErrorCode = ex.ErrorCode }),
            ForbiddenException ex => (StatusCodes.Status403Forbidden,
                new ApiError { Message = ex.Message, ErrorCode = "FORBIDDEN" }),
            NotFoundException ex => (StatusCodes.Status404NotFound,
                new ApiError { Message = ex.Message, ErrorCode = "NOT_FOUND" }),
            ConflictException ex => (StatusCodes.Status409Conflict,
                new ApiError { Message = ex.Message, ErrorCode = "CONFLICT" }),
            AiUnavailableException ex => (StatusCodes.Status503ServiceUnavailable,
                new ApiError { Message = ex.Message, ErrorCode = ex.ErrorCode }),
            _ => (StatusCodes.Status500InternalServerError,
                new ApiError { Message = "An unexpected error occurred.", ErrorCode = "INTERNAL_ERROR" })
        };
}