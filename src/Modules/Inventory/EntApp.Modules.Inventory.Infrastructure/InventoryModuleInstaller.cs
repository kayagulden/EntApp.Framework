using EntApp.Modules.Inventory.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.Inventory.Infrastructure;

/// <summary>Inventory modülü DI installer.</summary>
public sealed class InventoryModuleInstaller : IModuleInstaller
{
    public string ModuleName => "Inventory";

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", InventoryDbContext.Schema);
                    npgsql.MigrationsAssembly(typeof(InventoryDbContext).Assembly.FullName);
                }));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(InventoryModuleInstaller).Assembly));
    }
}
