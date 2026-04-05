using EntApp.Modules.MyModule.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.MyModule.Infrastructure;

/// <summary>MyModule modülü DI installer.</summary>
public sealed class MyModuleModuleInstaller : IModuleInstaller
{
    public string ModuleName => "MyModule";

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<MyModuleDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", MyModuleDbContext.Schema);
                    npgsql.MigrationsAssembly(typeof(MyModuleDbContext).Assembly.FullName);
                }));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(MyModuleModuleInstaller).Assembly));
    }
}
