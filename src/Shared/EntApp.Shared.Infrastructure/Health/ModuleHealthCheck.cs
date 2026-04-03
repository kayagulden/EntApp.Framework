using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.Health;

/// <summary>
/// Modül bazlı health check altyapısı.
/// Her modül kendi IModuleHealthCheck implementasyonunu register eder.
/// </summary>
public interface IModuleHealthCheck
{
    /// <summary>Modül adı.</summary>
    string ModuleName { get; }

    /// <summary>Modül sağlık durumunu kontrol eder.</summary>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// IModuleHealthCheck implementasyonlarını toplayan ASP.NET Core HealthCheck adapter.
/// </summary>
public sealed class ModuleHealthCheckAdapter : IHealthCheck
{
    private readonly IEnumerable<IModuleHealthCheck> _moduleChecks;
    private readonly ILogger<ModuleHealthCheckAdapter> _logger;

    public ModuleHealthCheckAdapter(
        IEnumerable<IModuleHealthCheck> moduleChecks,
        ILogger<ModuleHealthCheckAdapter> logger)
    {
        _moduleChecks = moduleChecks;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var overallHealthy = true;

        foreach (var check in _moduleChecks)
        {
            try
            {
                var result = await check.CheckHealthAsync(cancellationToken);
                data[check.ModuleName] = result.Status.ToString();

                if (result.Status != HealthStatus.Healthy)
                {
                    overallHealthy = false;
                    _logger.LogWarning(
                        "[HEALTH] Module {Module} is {Status}: {Description}",
                        check.ModuleName, result.Status, result.Description);
                }
            }
            catch (Exception ex)
            {
                overallHealthy = false;
                data[check.ModuleName] = $"Error: {ex.Message}";

                _logger.LogError(ex, "[HEALTH] Module {Module} check failed.", check.ModuleName);
            }
        }

        return overallHealthy
            ? HealthCheckResult.Healthy("All modules healthy.", data)
            : HealthCheckResult.Degraded("One or more modules degraded.", data: data);
    }
}
