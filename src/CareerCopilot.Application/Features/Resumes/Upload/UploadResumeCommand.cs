using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Application.Features.Resumes.Dtos;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Resumes.Upload;

public sealed record UploadResumeCommand(
    string FileName,
    string ContentType,
    Stream Content,
    bool SetDefault = false) : IRequest<ResumeDto>;

public sealed class UploadResumeCommandHandler : IRequestHandler<UploadResumeCommand, ResumeDto>
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".doc", ".txt"
    };

    public const int MaxFileSizeBytes = 10 * 1024 * 1024;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly IResumeParserService _resumeParser;

    public UploadResumeCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage,
        IResumeParserService resumeParser)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _resumeParser = resumeParser;
    }

    public async Task<ResumeDto> Handle(UploadResumeCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var extension = Path.GetExtension(request.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["file"] = new[] { "Only PDF, DOCX or text resumes are supported." }
            });
        }

        if (request.Content.Length > MaxFileSizeBytes)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["file"] = new[] { "Resume file must be 10 MB or smaller." }
            });
        }

        var storageReference = await _fileStorage.SaveAsync(
            request.Content, request.FileName, request.ContentType, cancellationToken);

        if (request.SetDefault)
        {
            var existingDefaults = _db.Set<Resume>().Where(r => r.UserId == userId && r.IsDefault);
            foreach (var r in existingDefaults)
            {
                r.IsDefault = false;
                _db.Update(r);
            }
        }

        var resume = new Resume
        {
            UserId = userId,
            Name = Path.GetFileNameWithoutExtension(request.FileName),
            OriginalFileName = request.FileName,
            FileType = extension.TrimStart('.').ToLowerInvariant(),
            StorageReference = storageReference,
            IsDefault = request.SetDefault || !(await _db.Set<Resume>().AnyAsync(r => r.UserId == userId, cancellationToken))
        };

        if (!string.Equals(resume.FileType, "txt", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                request.Content.Position = 0;
                var extraction = _resumeParser.TryExtractText(request.Content, request.FileName, request.ContentType);
                resume.ParsedText = extraction.Success ? extraction.Text : null;
                resume.ParseFailed = !extraction.Success;
            }
            catch
            {
                resume.ParseFailed = true;
            }
        }
        else
        {
            request.Content.Position = 0;
            using var reader = new StreamReader(request.Content);
            resume.ParsedText = await reader.ReadToEndAsync(cancellationToken);
        }

        _db.Add(resume);
        await _db.SaveChangesAsync(cancellationToken);

        return new ResumeDto(resume.Id, resume.Name, resume.OriginalFileName, resume.FileType,
            resume.IsDefault, resume.UploadedAt, false, null, null);
    }
}