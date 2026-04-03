using EntApp.Modules.FileManagement.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.FileManagement.Infrastructure.Storage;

/// <summary>
/// Local disk storage provider — dosyaları sunucu dosya sistemine yazar.
/// </summary>
public class LocalDiskStorageProvider : IStorageProvider
{
    private readonly string _basePath;
    private readonly ILogger<LocalDiskStorageProvider> _logger;

    public LocalDiskStorageProvider(string basePath, ILogger<LocalDiskStorageProvider> logger)
    {
        _basePath = basePath;
        _logger = logger;
        Directory.CreateDirectory(basePath);
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string? subfolder = null, CancellationToken ct = default)
    {
        var folder = subfolder is not null ? Path.Combine(_basePath, subfolder) : _basePath;
        Directory.CreateDirectory(folder);

        var uniqueName = $"{Guid.NewGuid():N}_{fileName}";
        var fullPath = Path.Combine(folder, uniqueName);

        await using var fileStream = File.Create(fullPath);
        await stream.CopyToAsync(fileStream, ct);

        var relativePath = Path.GetRelativePath(_basePath, fullPath);
        _logger.LogInformation("[Storage] Saved: {Path} ({Size} bytes)", relativePath, stream.Length);
        return relativePath;
    }

    public Task<Stream> GetAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Dosya bulunamadı: {storagePath}");

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("[Storage] Deleted: {Path}", storagePath);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        return Task.FromResult(File.Exists(fullPath));
    }
}
