namespace EntApp.Shared.Kernel.Results;

/// <summary>
/// Hata tipi. RFC 7807 ProblemDetails ile eşleşir.
/// </summary>
public enum ErrorType
{
    /// <summary>İş kuralı veya validasyon hatası (400)</summary>
    Validation,

    /// <summary>Kayıt bulunamadı (404)</summary>
    NotFound,

    /// <summary>Çakışma — duplicate, concurrency (409)</summary>
    Conflict,

    /// <summary>Yetki hatası (401/403)</summary>
    Unauthorized,

    /// <summary>Genel sunucu hatası (500)</summary>
    Failure
}
