using EntApp.Modules.Finance.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.Finance.Infrastructure;

/// <summary>Finance modülü DI installer.</summary>
public sealed class FinanceModuleInstaller : IModuleInstaller
{
    public string ModuleName => "Finance";

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<FinanceDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", FinanceDbContext.Schema);
                    npgsql.MigrationsAssembly(typeof(FinanceDbContext).Assembly.FullName);
                }));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(FinanceModuleInstaller).Assembly));
    }
}
