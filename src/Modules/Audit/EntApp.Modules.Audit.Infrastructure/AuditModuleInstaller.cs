using EntApp.Modules.Audit.Infrastructure.Interceptors;
using EntApp.Modules.Audit.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.Modularity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.Audit.Infrastructure;

public class AuditModuleInstaller : IModuleInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AuditDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AuditDbContext.Schema)));

        services.AddScoped<AuditSaveChangesInterceptor>();

        // MediatR handler'larını otomatik kaydet
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AuditModuleInstaller).Assembly));
    }
}
