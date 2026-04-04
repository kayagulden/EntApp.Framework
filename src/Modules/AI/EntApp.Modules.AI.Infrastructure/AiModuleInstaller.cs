using EntApp.Modules.AI.Application.Interfaces;
using EntApp.Modules.AI.Infrastructure.Persistence;
using EntApp.Modules.AI.Infrastructure.Services;
using EntApp.Shared.Contracts.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace EntApp.Modules.AI.Infrastructure;

/// <summary>
/// AI modülü DI installer — ModuleRegistration tarafından otomatik keşfedilir.
/// </summary>
public sealed class AiModuleInstaller : IModuleInstaller
{
    public string ModuleName => "AI";

    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // ── DbContext ────────────────────────────────────────
        services.AddDbContext<AiDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AiDbContext.Schema);
                    npgsql.MigrationsAssembly(typeof(AiDbContext).Assembly.FullName);
                }));

        // ── MediatR handlers ────────────────────────────────
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AiModuleInstaller).Assembly));

        // ── Semantic Kernel ─────────────────────────────────
        var aiSettings = configuration.GetSection("AiSettings");
        var defaultProvider = aiSettings.GetValue<string>("DefaultProvider") ?? "OpenAI";

        var kernelBuilder = services.AddKernel();

        // Provider'a göre chat completion ekle
        switch (defaultProvider)
        {
            case "AzureOpenAI":
                var azureEndpoint = aiSettings["AzureOpenAI:Endpoint"] ?? "";
                var azureKey = aiSettings["AzureOpenAI:ApiKey"] ?? "";
                var azureDeployment = aiSettings["AzureOpenAI:DeploymentName"] ?? "";
                if (!string.IsNullOrEmpty(azureKey))
                {
                    kernelBuilder.AddAzureOpenAIChatCompletion(azureDeployment, azureEndpoint, azureKey);
                }
                break;

            case "OpenAI":
            default:
                var openAiKey = aiSettings["OpenAI:ApiKey"] ?? "";
                var chatModel = aiSettings["OpenAI:ChatModel"] ?? "gpt-4o-mini";
                if (!string.IsNullOrEmpty(openAiKey))
                {
                    kernelBuilder.AddOpenAIChatCompletion(chatModel, openAiKey);
                }
                break;
        }

        // ── AI Services ─────────────────────────────────────
        services.AddScoped<ILlmService, SemanticKernelLlmService>();
        services.AddScoped<IPromptManager, ScribanPromptManager>();
        // IEmbeddingService, IRagService, IDocumentProcessor → 9b/9c'de eklenecek
    }
}
