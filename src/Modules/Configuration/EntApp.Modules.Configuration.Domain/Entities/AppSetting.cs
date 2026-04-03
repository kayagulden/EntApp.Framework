using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Configuration.Domain.Entities;

/// <summary>
/// Uygulama ayarı — key-value yapıda, tenant/global bazlı konfigürasyon.
/// </summary>
public class AppSetting : AuditableEntity<Guid>
{
    /// <summary>Ayar anahtarı (unique per tenant/global). Örn: "SmtpHost", "MaxUploadSizeMb"</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Ayar değeri (string olarak saklanır, gerekirse parse edilir).</summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>Değer tipi (String, Int, Bool, Json).</summary>
    public SettingValueType ValueType { get; private set; }

    /// <summary>Açıklama.</summary>
    public string? Description { get; private set; }

    /// <summary>Grup adı (ayarları kategorize etmek için). Örn: "Email", "Security", "General"</summary>
    public string? Group { get; private set; }

    /// <summary>null ise global, değilse tenant'a özel.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Şifreli mi? (connection string, API key vb.)</summary>
    public bool IsEncrypted { get; private set; }

    /// <summary>Sadece admin tarafından değiştirilebilir mi?</summary>
    public bool IsReadOnly { get; private set; }

    private AppSetting() { }

    public static AppSetting Create(
        string key, string value, SettingValueType valueType,
        string? description = null, string? group = null,
        Guid? tenantId = null, bool isEncrypted = false, bool isReadOnly = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Ayar anahtarı boş olamaz.", nameof(key));

        return new AppSetting
        {
            Id = Guid.NewGuid(),
            Key = key.Trim(),
            Value = value,
            ValueType = valueType,
            Description = description,
            Group = group,
            TenantId = tenantId,
            IsEncrypted = isEncrypted,
            IsReadOnly = isReadOnly
        };
    }

    public void UpdateValue(string newValue)
    {
        if (IsReadOnly)
            throw new InvalidOperationException($"'{Key}' ayarı salt okunurdur, değiştirilemez.");

        Value = newValue;
    }

    /// <summary>Değeri typed olarak döndürür.</summary>
    public T GetValue<T>()
    {
        return (T)Convert.ChangeType(Value, typeof(T));
    }

    public bool GetBoolValue() => bool.TryParse(Value, out var result) && result;
    public int GetIntValue() => int.TryParse(Value, out var result) ? result : 0;
}

public enum SettingValueType
{
    String,
    Int,
    Bool,
    Decimal,
    Json,
    ConnectionString
}
