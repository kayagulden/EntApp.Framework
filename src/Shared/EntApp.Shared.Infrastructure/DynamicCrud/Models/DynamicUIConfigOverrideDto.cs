using System.Text.Json.Serialization;

namespace EntApp.Shared.Infrastructure.DynamicCrud.Models;

/// <summary>
/// DB'den okunan UI override JSON'unun deserialize edildiği DTO.
/// DynamicUIConfig.ConfigJson alanından parse edilir.
/// </summary>
public sealed record DynamicUIConfigOverrideDto
{
    /// <summary>Entity başlığı override (örn: "Ülkeler").</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>İkon override (örn: "globe").</summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; init; }

    /// <summary>Aksiyon override'ları.</summary>
    [JsonPropertyName("actions")]
    public ActionOverrideDto? Actions { get; init; }

    /// <summary>
    /// Field seviyesinde override'lar.
    /// Key: field adı (camelCase), Value: override değerleri.
    /// </summary>
    [JsonPropertyName("fields")]
    public Dictionary<string, FieldOverrideDto>? Fields { get; init; }
}

/// <summary>
/// Tek bir field için UI override değerleri.
/// Null olan alanlar override edilmez (convention/attribute'den gelir).
/// </summary>
public sealed record FieldOverrideDto
{
    [JsonPropertyName("label")]
    public string? Label { get; init; }

    [JsonPropertyName("order")]
    public int? Order { get; init; }

    [JsonPropertyName("width")]
    public string? Width { get; init; }

    [JsonPropertyName("showInList")]
    public bool? ShowInList { get; init; }

    [JsonPropertyName("hidden")]
    public bool? Hidden { get; init; }

    [JsonPropertyName("searchable")]
    public bool? Searchable { get; init; }

    [JsonPropertyName("readOnly")]
    public bool? ReadOnly { get; init; }

    [JsonPropertyName("required")]
    public bool? Required { get; init; }
}

/// <summary>
/// Entity aksiyonları override.
/// </summary>
public sealed record ActionOverrideDto
{
    [JsonPropertyName("create")]
    public bool? Create { get; init; }

    [JsonPropertyName("edit")]
    public bool? Edit { get; init; }

    [JsonPropertyName("delete")]
    public bool? Delete { get; init; }

    [JsonPropertyName("export")]
    public bool? Export { get; init; }
}
