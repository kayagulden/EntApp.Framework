using System.Reflection;
using EntApp.Shared.Contracts.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.Modules;

/// <summary>
/// Convention-based modül auto-discovery ve registration.
/// IModuleInstaller implementasyonlarını assembly taraması ile bulur ve çalıştırır.
/// </summary>
public static class ModuleRegistration
{
    /// <summary>
    /// Verilen assembly'lerdeki tüm IModuleInstaller implementasyonlarını
    /// otomatik keşfeder ve Install metodlarını çalıştırır.
    /// </summary>
    public static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(assemblies);

        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetCallingAssembly()];
        }

        var installerType = typeof(IModuleInstaller);

        var installers = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => installerType.IsAssignableFrom(t)
                        && t is { IsAbstract: false, IsInterface: false })
            .Select(Activator.CreateInstance)
            .Cast<IModuleInstaller>()
            .OrderBy(i => i.ModuleName)
            .ToList();

        foreach (var installer in installers)
        {
            installer.Install(services, configuration);
        }

        // Keşfedilen modüllerin listesini DI'a kaydet (health check, diagnostik vb.)
        services.AddSingleton<IReadOnlyList<ModuleInfo>>(
            installers.Select(i => new ModuleInfo(i.ModuleName, i.GetType().Assembly.GetName().Name ?? "Unknown"))
                .ToList()
                .AsReadOnly());

        return services;
    }

    /// <summary>
    /// Yüklenen modülleri loglar.
    /// </summary>
    public static void LogLoadedModules(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var modules = app.Services.GetService<IReadOnlyList<ModuleInfo>>();
        var logger = app.Services.GetRequiredService<ILogger<ModuleInfo>>();

        if (modules is null || modules.Count == 0)
        {
            logger.LogWarning("[MODULES] No modules registered.");
            return;
        }

        logger.LogInformation("[MODULES] {Count} module(s) loaded:", modules.Count);
        foreach (var module in modules)
        {
            logger.LogInformation("  → {ModuleName} ({Assembly})", module.Name, module.Assembly);
        }
    }
}

/// <summary>Yüklü modül bilgisi.</summary>
/// <param name="Name">Modül adı</param>
/// <param name="Assembly">Assembly adı</param>
public sealed record ModuleInfo(string Name, string Assembly);
