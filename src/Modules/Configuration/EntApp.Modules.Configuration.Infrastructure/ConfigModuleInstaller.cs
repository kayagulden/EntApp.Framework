using EntApp.Modules.Configuration.Infrastructure.DynamicUI;
using EntApp.Modules.Configuration.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Modules;
using EntApp.Shared.Infrastructure.DynamicCrud;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.Configuration.Infrastructure;

public class ConfigModuleInstaller : IModuleInstaller
{
    public string ModuleName => "Configuration";

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ConfigDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ConfigDbContext.Schema)));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ConfigModuleInstaller).Assembly));

        // Dynamic UI konfigürasyon sağlayıcısı
        services.AddScoped<IDynamicUIConfigProvider, DynamicUIConfigProvider>();
    }
}
