using System.Text.Json.Serialization;

namespace EntApp.Shared.Infrastructure.DynamicCrud.Models;

/// <summary>
/// Entity metadata — frontend'e döndürülen JSON schema.
/// Attribute + convention-based bilgileri birleştirir.
/// </summary>
public sealed record EntityMetadataDto
{
    /// <summary>Entity adı (URL ve lookup'larda kullanılır).</summary>
    public required string Entity { get; init; }

    /// <summary>Görüntüleme başlığı (convention: entity adından türetilir).</summary>
    public required string Title { get; init; }

    /// <summary>Sidebar menu grubu.</summary>
    public string? MenuGroup { get; init; }

    /// <summary>Sidebar ikonu (ör: "globe", "map-pin").</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Icon { get; init; }

    /// <summary>Detail entity mi (master-detail alt tarafı)?</summary>
    public bool IsDetail { get; init; }

    /// <summary>Alan metadata listesi.</summary>
    public required IReadOnlyList<FieldMetadataDto> Fields { get; init; }

    /// <summary>Master-detail alt tablolar.</summary>
    public IReadOnlyList<DetailMetadataDto>? Details { get; init; }

    /// <summary>İzin verilen aksiyonlar.</summary>
    public required EntityActionsDto Actions { get; init; }
}

/// <summary>
/// Tek bir alan (property) metadata'sı.
/// </summary>
public sealed record FieldMetadataDto
{
    /// <summary>Property adı (camelCase).</summary>
    public required string Name { get; init; }

    /// <summary>Görüntüleme etiketi (convention: property adından türetilir).</summary>
    public required string Label { get; init; }

    /// <summary>Frontend component tipi.</summary>
    public required string Type { get; init; }

    /// <summary>Zorunlu mu?</summary>
    public bool Required { get; init; }

    /// <summary>Salt okunur mu?</summary>
    public bool ReadOnly { get; init; }

    /// <summary>Arama yapılabilir mi?</summary>
    public bool Searchable { get; init; }

    /// <summary>Maksimum uzunluk (0 = sınırsız).</summary>
    public int MaxLength { get; init; }

    /// <summary>Minimum uzunluk.</summary>
    public int MinLength { get; init; }

    /// <summary>Minimum değer (sayısal).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Min { get; init; }

    /// <summary>Maksimum değer (sayısal).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Max { get; init; }

    /// <summary>Varsayılan değer.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DefaultValue { get; init; }

    /// <summary>Hesaplanan alan ifadesi.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Computed { get; init; }

    /// <summary>Listede gösterilsin mi? (default: convention-based)</summary>
    public bool ShowInList { get; init; } = true;

    /// <summary>Sıralama (default: property sırası).</summary>
    public int Order { get; init; }

    /// <summary>Kolon genişliği (ör: "sm", "md", "lg", "xl").</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Width { get; init; }

    /// <summary>Gizli alan mı? (true ise metadata'dan çıkarılır).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Hidden { get; init; }

    /// <summary>Enum seçenekleri (enum tipli alanlar için).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Options { get; init; }

    /// <summary>Lookup bilgisi (lookup tipli alanlar için).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LookupInfoDto? Lookup { get; init; }
}

/// <summary>Lookup alan bilgisi.</summary>
public sealed record LookupInfoDto
{
    /// <summary>Lookup entity adı.</summary>
    public required string Entity { get; init; }

    /// <summary>Gösterilecek alan.</summary>
    public required string DisplayField { get; init; }

    /// <summary>Lookup endpoint URL'i.</summary>
    public required string Endpoint { get; init; }
}

/// <summary>Master-detail alt tablo metadata'sı.</summary>
public sealed record DetailMetadataDto
{
    /// <summary>Navigation property adı.</summary>
    public required string Name { get; init; }

    /// <summary>Görüntüleme etiketi.</summary>
    public required string Label { get; init; }

    /// <summary>Detail entity adı.</summary>
    public required string Entity { get; init; }

    /// <summary>Detail entity alanları.</summary>
    public required IReadOnlyList<FieldMetadataDto> Fields { get; init; }
}

/// <summary>Entity için izin verilen aksiyonlar.</summary>
public sealed record EntityActionsDto
{
    public bool Create { get; init; } = true;
    public bool Edit { get; init; } = true;
    public bool Delete { get; init; } = true;
    public bool Export { get; init; } = true;
}

/// <summary>Sidebar menu grubu.</summary>
public sealed record MenuGroupDto
{
    /// <summary>Grup adı.</summary>
    public required string Name { get; init; }

    /// <summary>Grup altındaki entity'ler.</summary>
    public required IReadOnlyList<MenuItemDto> Items { get; init; }
}

/// <summary>Sidebar menu öğesi.</summary>
public sealed record MenuItemDto
{
    /// <summary>Entity adı (route'da kullanılır).</summary>
    public required string Entity { get; init; }

    /// <summary>Görüntüleme başlığı.</summary>
    public required string Title { get; init; }
}
