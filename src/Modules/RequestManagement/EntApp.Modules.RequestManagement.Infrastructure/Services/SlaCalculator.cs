using System.Text.Json;
using EntApp.Modules.RequestManagement.Domain.Enums;

namespace EntApp.Modules.RequestManagement.Infrastructure.Services;

/// <summary>SLA deadline hesaplayıcı — öncelik bazlı yanıt ve çözüm sürelerini hesaplar.</summary>
public static class SlaCalculator
{
    /// <summary>SLA yanıt deadline'ını hesaplar.</summary>
    public static DateTime? CalculateResponseDeadline(string responseTimeJson, TicketPriority priority)
    {
        return CalculateDeadline(responseTimeJson, priority);
    }

    /// <summary>SLA çözüm deadline'ını hesaplar.</summary>
    public static DateTime? CalculateResolutionDeadline(string resolutionTimeJson, TicketPriority priority)
    {
        return CalculateDeadline(resolutionTimeJson, priority);
    }

    private static DateTime? CalculateDeadline(string json, TicketPriority priority)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
            return null;

        try
        {
            var map = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
            if (map is null) return null;

            var key = priority.ToString();
            return map.TryGetValue(key, out var minutes)
                ? DateTime.UtcNow.AddMinutes(minutes)
                : null;
        }
        catch
        {
            return null;
        }
    }
}
