namespace EntApp.Shared.Infrastructure.Persistence.Outbox;

/// <summary>
/// Outbox tablosu entity'si.
/// Integration event'ler önce bu tabloya yazılır,
/// sonra OutboxProcessor tarafından asenkron publish edilir.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Mesaj kimliği.</summary>
    public Guid Id { get; set; }

    /// <summary>Event tipi (full qualified type name).</summary>
    public required string Type { get; set; }

    /// <summary>Serialized event content (JSON).</summary>
    public required string Content { get; set; }

    /// <summary>Mesajın oluşturulma zamanı (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Mesajın işlenme zamanı. Null ise henüz işlenmemiş.</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>İşlenme sırasında hata oluştuysa hata mesajı.</summary>
    public string? Error { get; set; }

    /// <summary>Kaç kez denendiği.</summary>
    public int RetryCount { get; set; }
}
