using CareerCopilot.API.Common;
using CareerCopilot.Application.Features.Copilot.Chat;
using CareerCopilot.Application.Features.Copilot.Dtos;
using CareerCopilot.Application.Features.Copilot.Get;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/copilot")]
public sealed class CopilotController : ApiControllerBase
{
    [HttpPost("chat")]
    public async Task<ActionResult<CopilotReplyDto>> Chat(ChatRequest request, CancellationToken ct)
    {
        CopilotReplyDto reply;
        if (request.ConversationId.HasValue)
        {
            reply = await Mediator.Send(
                new SendCopilotMessageCommand(request.ConversationId.Value, request.Message), ct);
        }
        else
        {
            reply = await Mediator.Send(
                new StartCopilotConversationCommand(request.Message, request.JobId), ct);
        }

        return Ok(new SuccessResponse<CopilotReplyDto>(reply));
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<IReadOnlyList<CopilotConversationDto>>> Conversations(CancellationToken ct)
        => Ok(new SuccessResponse<IReadOnlyList<CopilotConversationDto>>(
            await Mediator.Send(new GetCopilotConversationsQuery(), ct)));

    [HttpGet("conversations/{id:guid}")]
    public async Task<ActionResult<IReadOnlyList<CopilotMessageDto>>> ConversationMessages(Guid id, CancellationToken ct)
        => Ok(new SuccessResponse<IReadOnlyList<CopilotMessageDto>>(
            await Mediator.Send(new GetCopilotMessagesQuery(id), ct)));

    [HttpDelete("conversations/{id:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteCopilotConversationCommand(id), ct);
        return Ok(new SuccessResponse<string>("Deleted."));
    }
}

public sealed record ChatRequest(string Message, Guid? JobId = null, Guid? ConversationId = null);