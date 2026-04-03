namespace EntApp.Shared.Infrastructure.Persistence.Outbox;

/// <summary>
/// İşlenmiş integration event kaydı.
/// Consumer tarafında idempotency sağlar — aynı event birden fazla işlenmez.
/// </summary>
public sealed class ProcessedEvent
{
    /// <summary>İşlenen event'in IdempotencyKey'i.</summary>
    public Guid IdempotencyKey { get; set; }

    /// <summary>Event tipi (full qualified type name).</summary>
    public required string EventType { get; set; }

    /// <summary>İşlenme zamanı (UTC).</summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
