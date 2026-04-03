using EntApp.Modules.FileManagement.Application.Abstractions;
using EntApp.Modules.FileManagement.Infrastructure.Persistence;
using EntApp.Modules.FileManagement.Infrastructure.Storage;
using EntApp.Shared.Infrastructure.Modularity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Minio;

namespace EntApp.Modules.FileManagement.Infrastructure;

public class FileModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<FileDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", FileDbContext.Schema)));

        // Storage Provider — config'e göre seçim
        var provider = configuration.GetValue<string>("FileStorage:Provider") ?? "LocalDisk";

        if (provider.Equals("MinIO", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            var endpoint = configuration["FileStorage:MinIO:Endpoint"] ?? "localhost:9000";
            var accessKey = configuration["FileStorage:MinIO:AccessKey"] ?? "minioadmin";
            var secretKey = configuration["FileStorage:MinIO:SecretKey"] ?? "minioadmin";
            var useSsl = configuration.GetValue<bool>("FileStorage:MinIO:UseSsl");
            var bucket = configuration["FileStorage:MinIO:Bucket"] ?? "entapp-files";

            services.AddSingleton<IMinioClient>(_ =>
            {
                var builder = new MinioClient()
                    .WithEndpoint(endpoint)
                    .WithCredentials(accessKey, secretKey);

                if (useSsl) builder = builder.WithSSL();
                return builder.Build();
            });

            services.AddSingleton<IStorageProvider>(sp =>
                new MinioStorageProvider(
                    sp.GetRequiredService<IMinioClient>(),
                    bucket,
                    sp.GetRequiredService<ILogger<MinioStorageProvider>>()));
        }
        else
        {
            // LocalDisk (varsayılan)
            var basePath = configuration.GetValue<string>("FileStorage:BasePath") ?? "uploads";
            services.AddSingleton<IStorageProvider>(sp =>
                new LocalDiskStorageProvider(basePath, sp.GetRequiredService<ILogger<LocalDiskStorageProvider>>()));
        }

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(FileModuleInstaller).Assembly));
    }
}
