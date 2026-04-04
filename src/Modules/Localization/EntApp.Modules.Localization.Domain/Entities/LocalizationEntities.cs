using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Localization.Domain.Entities;

/// <summary>
/// Desteklenen dil — kod, ad, varsayılan dil, aktif/pasif.
/// </summary>
public class Language : BaseEntity<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string NativeName { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int DisplayOrder { get; private set; }

    private Language() { }

    public static Language Create(string code, string name, string nativeName,
        bool isDefault = false, int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Dil kodu boş olamaz.", nameof(code));
        if (code.Length < 2 || code.Length > 10)
            throw new ArgumentException("Dil kodu 2-10 karakter arası olmalı.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Dil adı boş olamaz.", nameof(name));
        if (string.IsNullOrWhiteSpace(nativeName))
            throw new ArgumentException("Yerel dil adı boş olamaz.", nameof(nativeName));

        return new Language
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            NativeName = nativeName.Trim(),
            IsDefault = isDefault,
            IsActive = true,
            DisplayOrder = displayOrder
        };
    }

    public void SetAsDefault() => IsDefault = true;
    public void ClearDefault() => IsDefault = false;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void SetDisplayOrder(int order) => DisplayOrder = order;
}

/// <summary>
/// Çeviri kaydı — namespace.key bazlı, dil ve tenant destekli.
/// </summary>
public class TranslationEntry : BaseEntity<Guid>
{
    public string LanguageCode { get; private set; } = string.Empty;
    public string Namespace { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public bool IsVerified { get; private set; }
    public Guid? TenantId { get; private set; }
    public DateTime? LastModifiedAt { get; private set; }
    public string? LastModifiedBy { get; private set; }

    private TranslationEntry() { }

    public static TranslationEntry Create(string languageCode, string ns, string key, string value,
        Guid? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            throw new ArgumentException("Dil kodu boş olamaz.", nameof(languageCode));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Çeviri anahtarı boş olamaz.", nameof(key));

        return new TranslationEntry
        {
            Id = Guid.NewGuid(),
            LanguageCode = languageCode.Trim().ToLowerInvariant(),
            Namespace = ns?.Trim() ?? "common",
            Key = key.Trim(),
            Value = value ?? string.Empty,
            IsVerified = false,
            TenantId = tenantId,
            LastModifiedAt = DateTime.UtcNow
        };
    }

    public void UpdateValue(string value, string? modifiedBy = null)
    {
        Value = value;
        LastModifiedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void Verify() => IsVerified = true;
    public void Unverify() => IsVerified = false;

    public string FullKey => $"{Namespace}.{Key}";
}
