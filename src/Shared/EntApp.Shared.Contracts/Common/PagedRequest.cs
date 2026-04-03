namespace EntApp.Shared.Contracts.Common;

/// <summary>
/// Sayfalanmış sorgu isteği.
/// Tüm liste query'leri bu parametreleri alır.
/// </summary>
public record PagedRequest
{
    /// <summary>Sayfa numarası (1-based). Varsayılan: 1.</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>Sayfa başına kayıt sayısı. Varsayılan: 20. Maksimum: 100.</summary>
    public int PageSize { get; init; } = 20;

    /// <summary>Sıralama alanı (ör: "Name", "CreatedAt").</summary>
    public string? SortBy { get; init; }

    /// <summary>Azalan sıralama mı?</summary>
    public bool SortDescending { get; init; }

    /// <summary>Genel arama terimi.</summary>
    public string? SearchTerm { get; init; }

    /// <summary>Skip hesaplar (0-based).</summary>
    public int Skip => (Math.Max(1, PageNumber) - 1) * Math.Clamp(PageSize, 1, 100);

    /// <summary>Clamp'lenmiş sayfa boyutu (1-100).</summary>
    public int Take => Math.Clamp(PageSize, 1, 100);
}
