using CareerCopilot.API.Common;
using CareerCopilot.Application.Features.Interviews.Complete;
using CareerCopilot.Application.Features.Interviews.Create;
using CareerCopilot.Application.Features.Interviews.Dtos;
using CareerCopilot.Application.Features.Interviews.Get;
using CareerCopilot.Application.Features.Interviews.Submit;
using CareerCopilot.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/interviews")]
public sealed class InterviewsController : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<InterviewSessionDetailDto>> Create(CreateInterviewSessionRequest request, CancellationToken ct)
        => Ok(new SuccessResponse<InterviewSessionDetailDto>(
            await Mediator.Send(new CreateInterviewSessionCommand(request.JobId, request.Mode), ct)));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InterviewSessionDto>>> List(Guid jobId, CancellationToken ct)
        => Ok(new SuccessResponse<IReadOnlyList<InterviewSessionDto>>(
            await Mediator.Send(new GetInterviewSessionsQuery(jobId), ct)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InterviewSessionDetailDto>> Get(Guid id, CancellationToken ct)
        => Ok(new SuccessResponse<InterviewSessionDetailDto>(
            await Mediator.Send(new GetInterviewSessionQuery(id), ct)));

    [HttpPost("questions/{questionId:guid}/answer")]
    public async Task<ActionResult<SubmitInterviewAnswerDto>> SubmitAnswer(
        Guid questionId,
        SubmitAnswerRequest request,
        CancellationToken ct)
        => Ok(new SuccessResponse<SubmitInterviewAnswerDto>(
            await Mediator.Send(new SubmitInterviewAnswerCommand(questionId, request.Answer), ct)));

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<InterviewSessionDetailDto>> Complete(Guid id, CancellationToken ct)
        => Ok(new SuccessResponse<InterviewSessionDetailDto>(
            await Mediator.Send(new CompleteInterviewCommand(id), ct)));
}

public sealed record CreateInterviewSessionRequest(Guid JobId, InterviewMode Mode);

public sealed record SubmitAnswerRequest(string Answer);