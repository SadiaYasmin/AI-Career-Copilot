namespace CareerCopilot.Application.Common.Interfaces;

public sealed record ResumeExtractionResult(bool Success, string Text, string? Message);

/// <summary>
/// Extracts plain text from resume files (PDF / DOCX supported in MVP).
/// </summary>
public interface IResumeParserService
{
    ResumeExtractionResult TryExtractText(Stream stream, string fileName, string contentType);
}