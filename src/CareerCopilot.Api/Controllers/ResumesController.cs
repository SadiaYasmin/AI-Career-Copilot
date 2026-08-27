using CareerCopilot.API.Common;
using CareerCopilot.Application.Common.Models;
using CareerCopilot.Application.Features.Resumes.Analyze;
using CareerCopilot.Application.Features.Resumes.Content;
using CareerCopilot.Application.Features.Resumes.Delete;
using CareerCopilot.Application.Features.Resumes.Dtos;
using CareerCopilot.Application.Features.Resumes.Get;
using CareerCopilot.Application.Features.Resumes.SetDefault;
using CareerCopilot.Application.Features.Resumes.Upload;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CareerCopilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/resumes")]
public sealed class ResumesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ResumeDto>>> List(int page = 1, int pageSize = 20, CancellationToken ct = default)
        => Ok(new SuccessResponse<PagedResult<ResumeDto>>(await Mediator.Send(new GetResumesQuery(page, pageSize), ct)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ResumeDto>> Get(Guid id, CancellationToken ct)
        => Ok(new SuccessResponse<ResumeDto>(await Mediator.Send(new GetResumeQuery(id), ct)));

    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> Content(Guid id, CancellationToken ct)
    {
        var resume = await Mediator.Send(new GetResumeQuery(id), ct);
        var stream = await Mediator.Send(new GetResumeContentQuery(id), ct);
        return File(stream, MimeType.For(resume.FileType), resume.OriginalFileName);
    }

    [HttpPost]
    [RequestSizeLimit(11L * 1024 * 1024)]
    public async Task<ActionResult<ResumeDto>> Upload(
        [FromForm] IFormFile file,
        [FromForm] bool setDefault = false,
        CancellationToken ct = default)
    {
        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? MimeType.For(Path.GetExtension(file.FileName))
            : file.ContentType;

        await using var stream = file.OpenReadStream();
        var result = await Mediator.Send(
            new UploadResumeCommand(file.FileName, contentType, stream, setDefault), ct);
        return Ok(new SuccessResponse<ResumeDto>(result));
    }

    [HttpPost("{id:guid}/set-default")]
    public async Task<ActionResult<ResumeDto>> SetDefault(Guid id, CancellationToken ct)
        => Ok(new SuccessResponse<ResumeDto>(await Mediator.Send(new SetDefaultResumeCommand(id), ct)));

    [HttpPost("{id:guid}/analyze")]
    public async Task<ActionResult<ResumeAnalysisDto>> Analyze(Guid id, CancellationToken ct)
        => Ok(new SuccessResponse<ResumeAnalysisDto>(await Mediator.Send(new AnalyzeResumeCommand(id), ct)));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteResumeCommand(id), ct);
        return Ok(new SuccessResponse<string>("Deleted."));
    }
}

internal static class MimeType
{
    public static string For(string extensionOrType)
    {
        if (extensionOrType.Contains('/', StringComparison.Ordinal))
        {
            return extensionOrType;
        }

        return (extensionOrType.TrimStart('.').ToLowerInvariant()) switch
        {
            "pdf" => "application/pdf",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "doc" => "application/msword",
            _ => "text/plain"
        };
    }
}