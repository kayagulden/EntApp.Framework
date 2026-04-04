namespace EntApp.Shared.Kernel.Domain.Attributes;

/// <summary>
/// Field'in veri tipi ve validasyon kurallarını belirtir.
/// Sadece tip/validasyon bilgisi taşır — UI konfigürasyonları
/// (label, order, width, showInList) burada TUTULMAZ.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class DynamicFieldAttribute : Attribute
{
    /// <summary>
    /// Frontend'te render edilecek component tipi.
    /// Auto ise CLR tipinden otomatik türetilir.
    /// </summary>
    public FieldType FieldType { get; set; } = FieldType.Auto;

    /// <summary>Zorunlu alan mı?</summary>
    public bool Required { get; set; }

    /// <summary>Maksimum karakter uzunluğu (string alanlar için).</summary>
    public int MaxLength { get; set; }

    /// <summary>Minimum karakter uzunluğu (string alanlar için).</summary>
    public int MinLength { get; set; }

    /// <summary>Minimum değer (sayısal alanlar için).</summary>
    public double Min { get; set; } = double.MinValue;

    /// <summary>Maksimum değer (sayısal alanlar için).</summary>
    public double Max { get; set; } = double.MaxValue;

    /// <summary>Arama yapılabilir alan mı? (liste filtrelemede kullanılır).</summary>
    public bool Searchable { get; set; }

    /// <summary>Salt okunur mu? (formda düzenlenemez).</summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Varsayılan değer (string olarak aktarılır, runtime'da parse edilir).
    /// Örnek: "true", "0", "Draft"
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Hesaplanan alan ifadesi.
    /// Örnek: "Quantity * UnitPrice"
    /// Computed alanlar otomatik olarak readOnly olur.
    /// </summary>
    public string? Computed { get; set; }
}
