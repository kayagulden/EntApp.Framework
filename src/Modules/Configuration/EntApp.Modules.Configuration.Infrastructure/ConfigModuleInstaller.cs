using EntApp.Modules.Configuration.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Modules;
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
    }
}
