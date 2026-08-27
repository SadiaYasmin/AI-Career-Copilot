namespace CareerCopilot.Application.Common.Ai;

public sealed record CoverLetterContext(
    AiPersonSnapshot Person,
    AiJobSnapshot Job,
    string Length,
    string Tone);

public sealed record CoverLetterResult(string Content);