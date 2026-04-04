using EntApp.Modules.CRM.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.CRM.Infrastructure;

/// <summary>CRM modülü DI installer.</summary>
public sealed class CrmModuleInstaller : IModuleInstaller
{
    public string ModuleName => "CRM";

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<CrmDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", CrmDbContext.Schema);
                    npgsql.MigrationsAssembly(typeof(CrmDbContext).Assembly.FullName);
                }));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CrmModuleInstaller).Assembly));
    }
}
