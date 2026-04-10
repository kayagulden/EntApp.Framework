using EntApp.Modules.Workflow.Application.Interfaces;
using EntApp.Modules.Workflow.Infrastructure.Persistence;
using EntApp.Modules.Workflow.Infrastructure.Services;
using EntApp.Shared.Contracts.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Modules.Workflow.Infrastructure;

/// <summary>
/// Workflow modülü DI installer — ModuleRegistration tarafından otomatik keşfedilir.
/// NOT: Homegrown engine + Elsa v3 paralel çalışır.
/// Elsa kaydı Program.cs'de yapılır (AddElsa). Bu installer sadece homegrown servisleri kaydeder.
/// Custom activities bu assembly'de tanımlıdır ve AddActivitiesFrom ile otomatik keşfedilir.
/// </summary>
public sealed class WorkflowModuleInstaller : IModuleInstaller
{
    public string ModuleName => "Workflow";

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // ── DbContext ────────────────────────────────────────
        services.AddDbContext<WorkflowDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", WorkflowDbContext.Schema);
                    npgsql.MigrationsAssembly(typeof(WorkflowDbContext).Assembly.FullName);
                }));

        // ── MediatR handlers ────────────────────────────────
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(WorkflowModuleInstaller).Assembly));

        // ── Services ─────────────────────────────────────────
        services.AddScoped<IWorkflowEngine, WorkflowEngine>();

        // ── AI Workflow Service ──────────────────────────────
        services.AddScoped<IWorkflowAiService, WorkflowAiService>();

        // Named HttpClient for Elsa API calls (from WorkflowAiService)
        services.AddHttpClient("ElsaApi", client =>
        {
            client.BaseAddress = new Uri("http://localhost:5212/elsa/api/");
            client.DefaultRequestHeaders.Add("Authorization",
                "ApiKey 00000000-0000-0000-0000-000000000000");
        });
    }
}

