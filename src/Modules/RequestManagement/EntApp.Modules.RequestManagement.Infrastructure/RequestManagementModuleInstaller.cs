using EntApp.Modules.RequestManagement.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.RequestManagement.Infrastructure;

/// <summary>Request Management modülü DI installer.</summary>
public sealed class RequestManagementModuleInstaller : IModuleInstaller
{
    public string ModuleName => "RequestManagement";

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<RequestManagementDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", RequestManagementDbContext.Schema);
                    npgsql.MigrationsAssembly(typeof(RequestManagementDbContext).Assembly.FullName);
                }));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(RequestManagementModuleInstaller).Assembly));
    }
}
