using System.Text.Json;
using CareerCopilot.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace CareerCopilot.Infrastructure.Files;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string RootPath { get; set; } = "App_Data/uploads";
}

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IOptions<StorageOptions> options)
    {
        _rootPath = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        var reference = $"{Guid.NewGuid():N}{extension}";
        var contentPath = Path.Combine(_rootPath, reference);

        await using (var file = File.Create(contentPath))
        {
            await stream.CopyToAsync(file, cancellationToken);
        }

        var meta = new FileMeta(Guid.NewGuid(), fileName, contentType);
        var metaPath = Path.Combine(_rootPath, $"{reference}.json");
        await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(meta), cancellationToken);

        return reference;
    }

    public async Task<StoredFileData?> GetAsync(string reference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Contains("..", StringComparison.Ordinal))
        {
            return null;
        }

        var contentPath = Path.Combine(_rootPath, Path.GetFileName(reference));
        var metaPath = Path.Combine(_rootPath, $"{Path.GetFileName(reference)}.json");
        if (!File.Exists(contentPath))
        {
            return null;
        }

        FileMeta? meta = null;
        if (File.Exists(metaPath))
        {
            try
            {
                meta = JsonSerializer.Deserialize<FileMeta>(await File.ReadAllTextAsync(metaPath, cancellationToken));
            }
            catch (JsonException)
            {
                meta = null;
            }
        }

        var stream = File.OpenRead(contentPath);
        return new StoredFileData(stream, meta?.FileName ?? reference, meta?.ContentType ?? "application/octet-stream");
    }

    public Task DeleteAsync(string reference, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Contains("..", StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        var contentPath = Path.Combine(_rootPath, Path.GetFileName(reference));
        var metaPath = Path.Combine(_rootPath, $"{Path.GetFileName(reference)}.json");
        if (File.Exists(contentPath))
        {
            File.Delete(contentPath);
        }

        if (File.Exists(metaPath))
        {
            File.Delete(metaPath);
        }

        return Task.CompletedTask;
    }

    private sealed record FileMeta(Guid Id, string FileName, string ContentType);
}