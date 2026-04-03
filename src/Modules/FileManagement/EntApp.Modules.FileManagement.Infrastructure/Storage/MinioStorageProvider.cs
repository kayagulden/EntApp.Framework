using EntApp.Modules.FileManagement.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;

namespace EntApp.Modules.FileManagement.Infrastructure.Storage;

/// <summary>
/// MinIO / S3-compatible storage provider.
/// Hem MinIO hem de AWS S3, DigitalOcean Spaces, Backblaze B2 ile uyumlu.
/// </summary>
public class MinioStorageProvider : IStorageProvider
{
    private readonly IMinioClient _client;
    private readonly string _bucketName;
    private readonly ILogger<MinioStorageProvider> _logger;

    public MinioStorageProvider(IMinioClient client, string bucketName, ILogger<MinioStorageProvider> logger)
    {
        _client = client;
        _bucketName = bucketName;
        _logger = logger;
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string? subfolder = null, CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync(ct);

        var objectName = subfolder is not null
            ? $"{subfolder}/{Guid.NewGuid():N}_{fileName}"
            : $"{Guid.NewGuid():N}_{fileName}";

        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType("application/octet-stream"), ct);

        _logger.LogInformation("[MinIO] Saved: {Bucket}/{Object}", _bucketName, objectName);
        return objectName;
    }

    public async Task<Stream> GetAsync(string storagePath, CancellationToken ct = default)
    {
        var memoryStream = new MemoryStream();

        await _client.GetObjectAsync(new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(storagePath)
            .WithCallbackStream(async (stream, cancelToken) =>
            {
                await stream.CopyToAsync(memoryStream, cancelToken);
                memoryStream.Position = 0;
            }), ct);

        return memoryStream;
    }

    public async Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        await _client.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(storagePath), ct);

        _logger.LogInformation("[MinIO] Deleted: {Bucket}/{Object}", _bucketName, storagePath);
    }

    public async Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default)
    {
        try
        {
            await _client.StatObjectAsync(new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(storagePath), ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken ct)
    {
        var found = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName), ct);
        if (!found)
        {
            await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketName), ct);
            _logger.LogInformation("[MinIO] Bucket oluşturuldu: {Bucket}", _bucketName);
        }
    }
}
