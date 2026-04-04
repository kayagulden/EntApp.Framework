using EntApp.Modules.Notification.Application.Abstractions;
using EntApp.Modules.Notification.Infrastructure.Persistence;
using EntApp.Modules.Notification.Infrastructure.Providers;
using EntApp.Shared.Contracts.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.Notification.Infrastructure;

public class NotificationModuleInstaller : IModuleInstaller
{
    public string ModuleName => "Notification";

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", NotificationDbContext.Schema)));

        services.AddSingleton<ITemplateRenderer, SimpleTemplateRenderer>();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(NotificationModuleInstaller).Assembly));
    }
}
