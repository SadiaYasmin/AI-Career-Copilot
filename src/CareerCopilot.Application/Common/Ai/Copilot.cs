namespace CareerCopilot.Application.Common.Ai;

public sealed record CopilotContext(
    string Message,
    string ConversationTitle,
    AiPersonSnapshot Person,
    AiResumeSnapshot? Resume,
    AiJobSnapshot? Job,
    IReadOnlyList<string> RecentMessages,
    IReadOnlyDictionary<string, string> SupportingData);

public sealed record CopilotReply(string Content);