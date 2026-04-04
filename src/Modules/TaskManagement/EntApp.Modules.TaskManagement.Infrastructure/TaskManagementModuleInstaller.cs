using EntApp.Modules.TaskManagement.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.TaskManagement.Infrastructure;

/// <summary>TaskManagement modülü DI installer.</summary>
public sealed class TaskManagementModuleInstaller : IModuleInstaller
{
    public string ModuleName => "TaskManagement";

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<TaskManagementDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", TaskManagementDbContext.Schema);
                    npgsql.MigrationsAssembly(typeof(TaskManagementModuleInstaller).Assembly.FullName);
                }));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(TaskManagementModuleInstaller).Assembly));
    }
}
