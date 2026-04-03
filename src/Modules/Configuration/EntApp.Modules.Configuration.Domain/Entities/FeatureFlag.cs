using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Configuration.Domain.Entities;

/// <summary>
/// Feature flag — özellik açma/kapama, bakım modu, A/B test desteği.
/// </summary>
public class FeatureFlag : AuditableEntity<Guid>
{
    /// <summary>Flag adı (unique). Örn: "MaintenanceMode", "NewDashboard", "BetaFeature"</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Görünen ad.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Açıklama.</summary>
    public string? Description { get; private set; }

    /// <summary>Aktif mi?</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>null ise global, değilse tenant'a özel.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Başlangıç tarihi (scheduled enable).</summary>
    public DateTime? EnabledFrom { get; private set; }

    /// <summary>Bitiş tarihi (scheduled disable).</summary>
    public DateTime? EnabledUntil { get; private set; }

    /// <summary>Sadece belirli roller için mi? (JSON array) Örn: ["Admin","Manager"]</summary>
    public string? AllowedRoles { get; private set; }

    /// <summary>Ek metadata (JSON).</summary>
    public string? Metadata { get; private set; }

    private FeatureFlag() { }

    public static FeatureFlag Create(
        string name, string displayName, string? description = null,
        bool isEnabled = false, Guid? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Flag adı boş olamaz.", nameof(name));

        return new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            DisplayName = displayName,
            Description = description,
            IsEnabled = isEnabled,
            TenantId = tenantId
        };
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
    public void Toggle() => IsEnabled = !IsEnabled;

    public void SetSchedule(DateTime? from, DateTime? until)
    {
        EnabledFrom = from;
        EnabledUntil = until;
    }

    public void SetAllowedRoles(string? rolesJson) => AllowedRoles = rolesJson;

    /// <summary>Şu anki durumda aktif mi? (schedule dahil)</summary>
    public bool IsEffectivelyEnabled()
    {
        if (!IsEnabled) return false;

        var now = DateTime.UtcNow;
        if (EnabledFrom.HasValue && now < EnabledFrom.Value) return false;
        if (EnabledUntil.HasValue && now > EnabledUntil.Value) return false;

        return true;
    }
}
