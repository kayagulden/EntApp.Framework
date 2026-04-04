using EntApp.Modules.AI.Domain.Enums;
using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.AI.Domain.Entities;

/// <summary>
/// Her LLM/Embedding çağrısının kullanım kaydı.
/// Maliyet takibi ve rate limiting için kullanılır.
/// </summary>
public sealed class AiUsageLog : BaseEntity<Guid>, ITenantEntity
{
    /// <summary>Kullanılan model kaydının ID'si</summary>
    public Guid? ModelId { get; private set; }

    /// <summary>Sağlayıcı</summary>
    public AiProvider Provider { get; private set; }

    /// <summary>Model adı (ör: gpt-4o-mini)</summary>
    public string ModelName { get; private set; } = string.Empty;

    /// <summary>Operasyon tipi</summary>
    public AiOperation Operation { get; private set; }

    /// <summary>Giriş token sayısı</summary>
    public int InputTokens { get; private set; }

    /// <summary>Çıkış token sayısı</summary>
    public int OutputTokens { get; private set; }

    /// <summary>Toplam token</summary>
    public int TotalTokens { get; private set; }

    /// <summary>Tahmini maliyet (USD)</summary>
    public decimal EstimatedCost { get; private set; }

    /// <summary>Süre (ms)</summary>
    public long DurationMs { get; private set; }

    /// <summary>Başarılı mı?</summary>
    public bool IsSuccess { get; private set; }

    /// <summary>Hata mesajı (başarısızsa)</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Çağrıyı yapan modül</summary>
    public string? ModuleName { get; private set; }

    /// <summary>Çağrıyı yapan kullanıcı</summary>
    public Guid? UserId { get; private set; }

    public Guid TenantId { get; set; }

    private AiUsageLog() { }

    public static AiUsageLog CreateSuccess(
        AiProvider provider,
        string modelName,
        AiOperation operation,
        int inputTokens,
        int outputTokens,
        decimal estimatedCost,
        long durationMs,
        Guid? modelId = null,
        string? moduleName = null,
        Guid? userId = null)
    {
        return new AiUsageLog
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            ModelName = modelName,
            ModelId = modelId,
            Operation = operation,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            TotalTokens = inputTokens + outputTokens,
            EstimatedCost = estimatedCost,
            DurationMs = durationMs,
            IsSuccess = true,
            ModuleName = moduleName,
            UserId = userId,
        };
    }

    public static AiUsageLog CreateFailure(
        AiProvider provider,
        string modelName,
        AiOperation operation,
        string errorMessage,
        long durationMs,
        string? moduleName = null,
        Guid? userId = null)
    {
        return new AiUsageLog
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            ModelName = modelName,
            Operation = operation,
            IsSuccess = false,
            ErrorMessage = errorMessage,
            DurationMs = durationMs,
            ModuleName = moduleName,
            UserId = userId,
        };
    }
}
