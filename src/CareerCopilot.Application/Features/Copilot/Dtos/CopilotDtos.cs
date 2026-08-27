using CareerCopilot.Domain.Enums;

namespace CareerCopilot.Application.Features.Copilot.Dtos;

public sealed record CopilotMessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTime CreatedAt);

public sealed record CopilotConversationDto(
    Guid Id,
    string Title,
    int MessageCount,
    DateTime LastActivityAt);

public sealed record CopilotReplyDto(
    Guid ConversationId,
    CopilotMessageDto Message);

public sealed record StartCopilotConversationCommand(
    string Message,
    Guid? JobId = null) : MediatR.IRequest<CopilotReplyDto>;

public sealed record SendCopilotMessageCommand(
    Guid ConversationId,
    string Message) : MediatR.IRequest<CopilotReplyDto>;

public sealed record DeleteCopilotConversationCommand(Guid Id) : MediatR.IRequest<MediatR.Unit>;

public sealed record GetCopilotMessagesQuery(Guid ConversationId) : MediatR.IRequest<IReadOnlyList<CopilotMessageDto>>;