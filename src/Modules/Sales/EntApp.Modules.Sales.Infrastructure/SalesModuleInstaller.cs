using EntApp.Modules.Sales.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.Sales.Infrastructure;

/// <summary>Sales modülü DI installer.</summary>
public sealed class SalesModuleInstaller : IModuleInstaller
{
    public string ModuleName => "Sales";

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<SalesDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", SalesDbContext.Schema);
                    npgsql.MigrationsAssembly(typeof(SalesDbContext).Assembly.FullName);
                }));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(SalesModuleInstaller).Assembly));
    }
}
