using EntApp.Shared.Kernel.Domain;
using EntApp.Shared.Kernel.Domain.Attributes;

namespace EntApp.Modules.Configuration.Domain.Entities;

/// <summary>
/// Şehir tanımı — Country FK ile lookup testi.
/// </summary>
[DynamicEntity("City", MenuGroup = "Tanımlar")]
public class City : BaseEntity<Guid>
{
    /// <summary>Şehir adı.</summary>
    [DynamicField(Required = true, MaxLength = 100, Searchable = true)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Plaka kodu (ör: 34, 06).</summary>
    [DynamicField(MaxLength = 10)]
    public string? PlateCode { get; set; }

    /// <summary>Ülke FK — lookup combobox olarak render edilir.</summary>
    [DynamicLookup(typeof(Country))]
    public Guid CountryId { get; set; }

    /// <summary>Navigation property.</summary>
    public Country? Country { get; set; }

    /// <summary>Aktif mi?</summary>
    [DynamicField]
    public bool IsActive { get; set; } = true;
}
