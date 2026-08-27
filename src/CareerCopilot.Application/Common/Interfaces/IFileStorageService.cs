namespace CareerCopilot.Application.Common.Interfaces;

public sealed record StoredFile(string Reference);

public sealed record StoredFileData(Stream Stream, string FileName, string ContentType);

/// <summary>
/// Abstraction over file storage. Files are private by default and only retrievable
/// through the owning application flow - never via public URLs.
/// </summary>
public interface IFileStorageService
{
    Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<StoredFileData?> GetAsync(string reference, CancellationToken cancellationToken = default);
    Task DeleteAsync(string reference, CancellationToken cancellationToken = default);
}