using EntApp.Modules.IAM.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.IAM.Infrastructure;

/// <summary>
/// IAM modülü DI installer — ModuleRegistration tarafından otomatik keşfedilir.
/// </summary>
public sealed class IamModuleInstaller : IModuleInstaller
{
    public string ModuleName => "IAM";

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // ── DbContext ────────────────────────────────────────
        services.AddDbContext<IamDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", IamDbContext.Schema);
                    npgsql.MigrationsAssembly(typeof(IamDbContext).Assembly.FullName);
                }));

        // ── MediatR handlers (bu assembly'den) ──────────────
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(IamModuleInstaller).Assembly));
    }
}
