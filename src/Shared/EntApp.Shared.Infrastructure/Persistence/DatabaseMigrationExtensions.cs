using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.Persistence;

/// <summary>
/// Startup'ta her modülün DbContext'ine migration uygulayan extension metod.
/// Her modül kendi Migrations/ klasöründe migration dosyalarını barındırır.
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Belirtilen DbContext için bekleyen migration'ları uygular.
    /// </summary>
    /// <typeparam name="TContext">Modülün DbContext tipi</typeparam>
    /// <example>
    /// <code>
    /// // Program.cs — app.Build() sonrası
    /// await app.MigrateDatabaseAsync&lt;IAMDbContext&gt;();
    /// await app.MigrateDatabaseAsync&lt;CMSDbContext&gt;();
    /// </code>
    /// </example>
    public static async Task MigrateDatabaseAsync<TContext>(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();
        var contextName = typeof(TContext).Name;

        try
        {
            var pendingMigrations = await context.Database
                .GetPendingMigrationsAsync(cancellationToken);

            var migrationList = pendingMigrations.ToList();

            if (migrationList.Count == 0)
            {
                logger.LogInformation("[MIGRATION] {Context}: No pending migrations.", contextName);
                return;
            }

            logger.LogInformation(
                "[MIGRATION] {Context}: Applying {Count} pending migration(s)...",
                contextName, migrationList.Count);

            foreach (var migration in migrationList)
            {
                logger.LogDebug("[MIGRATION] {Context}: → {Migration}", contextName, migration);
            }

            await context.Database.MigrateAsync(cancellationToken);

            logger.LogInformation(
                "[MIGRATION] {Context}: All migrations applied successfully.", contextName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[MIGRATION] {Context}: Migration failed!", contextName);
            throw;
        }
    }
}
