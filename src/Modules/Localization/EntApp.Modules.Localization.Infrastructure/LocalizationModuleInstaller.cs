using EntApp.Modules.Localization.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.Modularity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.Localization.Infrastructure;

public class LocalizationModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<LocalizationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", LocalizationDbContext.Schema)));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(LocalizationModuleInstaller).Assembly));
    }
}
