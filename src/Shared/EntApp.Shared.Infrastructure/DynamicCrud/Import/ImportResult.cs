namespace EntApp.Shared.Infrastructure.DynamicCrud.Import;

/// <summary>
/// Import işlemi sonuç DTO'su.
/// </summary>
public sealed class ImportResult
{
    public int SuccessCount { get; init; }
    public int ErrorCount { get; init; }
    public int TotalCount { get; init; }
    public List<ImportError> Errors { get; init; } = [];
}

/// <summary>
/// Import hatası — satır bazında.
/// </summary>
public sealed class ImportError
{
    public int RowNumber { get; init; }
    public string Field { get; init; } = "";
    public string Message { get; init; } = "";
}

/// <summary>
/// Import önizleme yanıtı.
/// </summary>
public sealed class ImportPreview
{
    /// <summary>Excel/CSV dosyasındaki kolon başlıkları.</summary>
    public List<string> FileHeaders { get; init; } = [];

    /// <summary>Entity metadata field'ları (eşleştirme hedefi).</summary>
    public List<ImportFieldInfo> EntityFields { get; init; } = [];

    /// <summary>Otomatik eşleştirme önerisi: fileHeader index → field name.</summary>
    public Dictionary<int, string> SuggestedMapping { get; init; } = new();

    /// <summary>İlk N satır preview.</summary>
    public List<List<string>> PreviewRows { get; init; } = [];

    public int TotalRowCount { get; init; }
}

/// <summary>
/// Import eşleştirmesinde kullanılacak entity field bilgisi.
/// </summary>
public sealed class ImportFieldInfo
{
    public string Name { get; init; } = "";
    public string Label { get; init; } = "";
    public string Type { get; init; } = "";
    public bool Required { get; init; }
}
