using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.Persistence;

/// <summary>
/// Seed data altyapısı.
/// Her modül kendi seed data provider'larını register eder.
/// Startup'ta sıralı olarak çalışır.
/// </summary>
public interface ISeedDataProvider
{
    /// <summary>Seed provider önceliği (düşük = önce çalışır).</summary>
    int Order { get; }

    /// <summary>Seed data sağlayıcı adı.</summary>
    string Name { get; }

    /// <summary>Seed data uygular. İdempotent olmalıdır.</summary>
    Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}

/// <summary>
/// Startup'ta tüm ISeedDataProvider implementasyonlarını sıralı çalıştırır.
/// </summary>
public static class SeedDataExtensions
{
    /// <summary>
    /// Tüm register edilmiş seed data provider'ları sıralı çalıştırır.
    /// </summary>
    /// <example>
    /// <code>
    /// // Program.cs
    /// if (app.Environment.IsDevelopment())
    /// {
    ///     await app.Services.SeedDatabaseAsync();
    /// }
    /// </code>
    /// </example>
    public static async Task SeedDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();
        var providers = scope.ServiceProvider
            .GetServices<ISeedDataProvider>()
            .OrderBy(p => p.Order)
            .ToList();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ISeedDataProvider>>();

        if (providers.Count == 0)
        {
            logger.LogInformation("[SEED] No seed data providers registered.");
            return;
        }

        logger.LogInformation("[SEED] Running {Count} seed data provider(s)...", providers.Count);

        foreach (var provider in providers)
        {
            try
            {
                logger.LogInformation("[SEED] → {Name} (order: {Order})", provider.Name, provider.Order);
                await provider.SeedAsync(scope.ServiceProvider, cancellationToken);
                logger.LogInformation("[SEED] ✓ {Name} completed.", provider.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SEED] ✗ {Name} failed!", provider.Name);
                throw;
            }
        }

        logger.LogInformation("[SEED] All seed data applied successfully.");
    }
}
