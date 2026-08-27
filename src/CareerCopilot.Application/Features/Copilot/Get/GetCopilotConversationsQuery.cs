using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Copilot.Dtos;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Copilot.Get
{

public sealed record GetCopilotConversationsQuery : IRequest<IReadOnlyList<CopilotConversationDto>>;

public sealed class GetCopilotConversationsQueryHandler
    : IRequestHandler<GetCopilotConversationsQuery, IReadOnlyList<CopilotConversationDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCopilotConversationsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CopilotConversationDto>> Handle(
        GetCopilotConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var items = await _db.Set<CopilotConversation>()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new CopilotConversationDto(
                c.Id,
                c.Title ?? "Career Copilot chat",
                c.Messages.Count,
                c.UpdatedAt))
            .ToListAsync(cancellationToken);

        return items;
    }
}

public sealed class GetCopilotMessagesQueryHandler : IRequestHandler<GetCopilotMessagesQuery, IReadOnlyList<CopilotMessageDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCopilotMessagesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CopilotMessageDto>> Handle(
        GetCopilotMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var ownsConversation = await _db.Set<CopilotConversation>()
            .AnyAsync(c => c.Id == request.ConversationId && c.UserId == userId, cancellationToken);

        if (!ownsConversation)
        {
            throw new NotFoundException("Conversation not found.");
        }

        return await _db.Set<CopilotMessage>()
            .Where(m => m.ConversationId == request.ConversationId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new CopilotMessageDto(m.Id, m.Role.ToString(), m.Content, m.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

}

namespace CareerCopilot.Application.Features.Copilot.Delete
{
    public sealed class DeleteCopilotConversationCommandHandler : IRequestHandler<DeleteCopilotConversationCommand, MediatR.Unit>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public DeleteCopilotConversationCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<MediatR.Unit> Handle(DeleteCopilotConversationCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var conversation = await _db.Set<CopilotConversation>()
                .Where(c => c.Id == request.Id && c.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Conversation not found.");

            var messages = _db.Set<CopilotMessage>().Where(m => m.ConversationId == conversation.Id);
            _db.RemoveRange(messages);
            _db.Remove(conversation);
            await _db.SaveChangesAsync(cancellationToken);

            return MediatR.Unit.Value;
        }
    }
}