namespace EntApp.Shared.Contracts.Common;

/// <summary>
/// Sayfalanmış sorgu sonucu.
/// Tüm liste endpoint'leri bu format ile döner.
/// </summary>
public sealed record PagedResult<T>
{
    /// <summary>Mevcut sayfa verileri.</summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>Toplam kayıt sayısı (filtrelenmiş).</summary>
    public required int TotalCount { get; init; }

    /// <summary>Sayfa numarası (1-based).</summary>
    public required int PageNumber { get; init; }

    /// <summary>Sayfa başına kayıt sayısı.</summary>
    public required int PageSize { get; init; }

    /// <summary>Toplam sayfa sayısı.</summary>
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)TotalCount / PageSize)
        : 0;

    /// <summary>Sonraki sayfa var mı?</summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>Önceki sayfa var mı?</summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>Boş sayfalanmış sonuç.</summary>
    public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 20)
        => new()
        {
            Items = [],
            TotalCount = 0,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
}
