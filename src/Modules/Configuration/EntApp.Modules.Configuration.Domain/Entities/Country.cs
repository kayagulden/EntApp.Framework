using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.Configuration.Domain.Entities;

/// <summary>
/// Ülke tanımı — ISO 3166 standardında.
/// Dynamic CRUD test entity'si.
/// </summary>
[DynamicEntity("Country", MenuGroup = "Tanımlar")]
public class Country : BaseEntity<Guid>
{
    /// <summary>ISO 3166-1 alpha-2/3 kodu (ör: TR, USA).</summary>
    [DynamicField(Required = true, MaxLength = 3, Searchable = true)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Ülke adı.</summary>
    [DynamicField(Required = true, MaxLength = 100, Searchable = true)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Telefon kodu (ör: +90).</summary>
    [DynamicField(MaxLength = 5)]
    public string? PhoneCode { get; set; }

    /// <summary>Aktif mi?</summary>
    [DynamicField]
    public bool IsActive { get; set; } = true;
}
