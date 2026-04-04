using EntApp.Modules.Procurement.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.Procurement.Infrastructure;

/// <summary>Procurement modülü DI installer.</summary>
public sealed class ProcurementModuleInstaller : IModuleInstaller
{
    public string ModuleName => "Procurement";

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<ProcurementDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ProcurementDbContext.Schema);
                    npgsql.MigrationsAssembly(typeof(ProcurementDbContext).Assembly.FullName);
                }));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ProcurementModuleInstaller).Assembly));
    }
}
