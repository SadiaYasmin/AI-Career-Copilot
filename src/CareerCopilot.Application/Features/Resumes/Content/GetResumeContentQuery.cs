using CareerCopilot.Application.Common.Exceptions;
using CareerCopilot.Application.Common.Interfaces;
using CareerCopilot.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Application.Features.Resumes.Content;

public sealed record GetResumeContentQuery(Guid Id) : IRequest<MemoryStream>;

public sealed class GetResumeContentQueryHandler : IRequestHandler<GetResumeContentQuery, MemoryStream>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;

    public GetResumeContentQueryHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<MemoryStream> Handle(GetResumeContentQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedException("Authentication required.");

        var resume = await _db.Set<Resume>()
            .Where(r => r.Id == request.Id && r.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Resume not found.");

        var stored = await _fileStorage.GetAsync(resume.StorageReference, cancellationToken);
        if (stored is null)
        {
            throw new NotFoundException("Resume file is no longer available.");
        }

        using var stream = stored.Stream;
        var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;
        return memory;
    }
}