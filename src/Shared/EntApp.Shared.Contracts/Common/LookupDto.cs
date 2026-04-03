namespace EntApp.Shared.Contracts.Common;

/// <summary>
/// Dropdown/combobox için lookup DTO.
/// Tüm entity lookup endpoint'leri bu format ile döner.
/// </summary>
public sealed record LookupDto
{
    /// <summary>Kayıt kimliği.</summary>
    public required Guid Id { get; init; }

    /// <summary>Görüntülenecek metin.</summary>
    public required string Text { get; init; }

    /// <summary>Opsiyonel açıklama veya alt metin.</summary>
    public string? Description { get; init; }

    /// <summary>Aktif/pasif durumu (pasifler gri gösterilir).</summary>
    public bool IsActive { get; init; } = true;
}
