using EntApp.Modules.MultiTenancy.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.Modularity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.MultiTenancy.Infrastructure;

public class TenantModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<TenantDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", TenantDbContext.Schema)));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(TenantModuleInstaller).Assembly));
    }
}
