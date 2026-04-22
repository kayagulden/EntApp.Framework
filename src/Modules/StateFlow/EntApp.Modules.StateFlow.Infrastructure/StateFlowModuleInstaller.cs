using EntApp.Modules.StateFlow.Application.Interfaces;
using EntApp.Modules.StateFlow.Infrastructure.Persistence;
using EntApp.Modules.StateFlow.Infrastructure.Services;
using EntApp.Shared.Contracts.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.StateFlow.Infrastructure;

/// <summary>StateFlow modülü DI installer.</summary>
public sealed class StateFlowModuleInstaller : IModuleInstaller
{
    public string ModuleName => "StateFlow";

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<StateFlowDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", StateFlowDbContext.Schema);
                    npgsql.MigrationsAssembly(typeof(StateFlowDbContext).Assembly.FullName);
                }));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(StateFlowModuleInstaller).Assembly));

        // Runtime engine — Stateless entegrasyonu
        services.AddScoped<IStateFlowEngine, StateFlowEngine>();
    }
}
