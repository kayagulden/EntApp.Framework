using EntApp.Modules.Notification.Application.Abstractions;
using EntApp.Modules.Notification.Infrastructure.Persistence;
using EntApp.Modules.Notification.Infrastructure.Providers;
using EntApp.Shared.Infrastructure.Modularity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.Notification.Infrastructure;

public class NotificationModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", NotificationDbContext.Schema)));

        // Providers
        services.AddSingleton<ITemplateRenderer, SimpleTemplateRenderer>();
        services.AddScoped<INotificationSender, SmtpNotificationSender>();
        services.AddScoped<INotificationSender, InAppNotificationSender>();

        // MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(NotificationModuleInstaller).Assembly));
    }
}
