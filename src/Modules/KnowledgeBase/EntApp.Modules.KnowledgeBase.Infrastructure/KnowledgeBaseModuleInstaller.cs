using EntApp.Modules.KnowledgeBase.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.KnowledgeBase.Infrastructure;

/// <summary>KnowledgeBase modülü DI installer.</summary>
public sealed class KnowledgeBaseModuleInstaller : IModuleInstaller
{
    public string ModuleName => "KnowledgeBase";

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<KnowledgeBaseDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", KnowledgeBaseDbContext.Schema);
                    npgsql.MigrationsAssembly(typeof(KnowledgeBaseModuleInstaller).Assembly.FullName);
                }));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(KnowledgeBaseModuleInstaller).Assembly));
    }
}
