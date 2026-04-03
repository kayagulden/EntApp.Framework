using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.MultiTenancy.Domain.Entities;

public enum TenantStatus
{
    Active,
    Suspended,
    PendingSetup,
    Deactivated
}

/// <summary>
/// Tenant — kiracı bilgileri, plan, durum, subdomain.
/// </summary>
public class Tenant : AuditableEntity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Identifier { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public string? Description { get; private set; }
    public string? Subdomain { get; private set; }
    public string? ConnectionString { get; private set; }
    public TenantStatus Status { get; private set; }
    public string Plan { get; private set; } = "Free";
    public DateTime? ActivatedAt { get; private set; }
    public DateTime? SuspendedAt { get; private set; }
    public string? AdminEmail { get; private set; }
    public string? LogoUrl { get; private set; }

    private readonly List<TenantSetting> _settings = new();
    public IReadOnlyCollection<TenantSetting> Settings => _settings.AsReadOnly();

    private Tenant() { }

    public static Tenant Create(string name, string identifier, string? adminEmail = null,
        string? displayName = null, string? description = null, string plan = "Free")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant adı boş olamaz.", nameof(name));
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Tenant tanımlayıcı boş olamaz.", nameof(identifier));
        if (identifier.Length < 3 || identifier.Length > 50)
            throw new ArgumentException("Tanımlayıcı 3-50 karakter arası olmalı.", nameof(identifier));

        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Identifier = identifier.Trim().ToLowerInvariant(),
            DisplayName = displayName,
            Description = description,
            AdminEmail = adminEmail,
            Plan = plan,
            Status = TenantStatus.PendingSetup
        };
    }

    public void Activate()
    {
        Status = TenantStatus.Active;
        ActivatedAt = DateTime.UtcNow;
        SuspendedAt = null;
    }

    public void Suspend(string? reason = null)
    {
        Status = TenantStatus.Suspended;
        SuspendedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = TenantStatus.Deactivated;
    }

    public void UpdateInfo(string? displayName, string? description, string? logoUrl)
    {
        DisplayName = displayName;
        Description = description;
        LogoUrl = logoUrl;
    }

    public void SetSubdomain(string subdomain)
    {
        Subdomain = subdomain?.Trim().ToLowerInvariant();
    }

    public void SetConnectionString(string? connectionString)
    {
        ConnectionString = connectionString;
    }

    public void ChangePlan(string plan)
    {
        if (string.IsNullOrWhiteSpace(plan))
            throw new ArgumentException("Plan boş olamaz.", nameof(plan));
        Plan = plan;
    }

    public void AddSetting(string key, string value)
    {
        var existing = _settings.FirstOrDefault(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.UpdateValue(value);
            return;
        }
        _settings.Add(TenantSetting.Create(Id, key, value));
    }

    public string? GetSetting(string key)
    {
        return _settings.FirstOrDefault(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    public bool IsActive => Status == TenantStatus.Active;
}

/// <summary>
/// Tenant'a özgü ayar (key-value).
/// </summary>
public class TenantSetting : BaseEntity<Guid>
{
    public Guid TenantId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;

    private TenantSetting() { }

    public static TenantSetting Create(Guid tenantId, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Ayar anahtarı boş olamaz.", nameof(key));

        return new TenantSetting
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Key = key.Trim(),
            Value = value
        };
    }

    public void UpdateValue(string value) => Value = value;
}
