using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntApp.Shared.Infrastructure.Persistence.Outbox;

/// <summary>
/// ProcessedEvent EF Core konfigürasyonu.
/// </summary>
public sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    private readonly string _schema;

    public ProcessedEventConfiguration(string schema = "app")
    {
        _schema = schema;
    }

    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("processed_events", _schema);

        builder.HasKey(x => x.IdempotencyKey);
        builder.Property(x => x.IdempotencyKey).ValueGeneratedNever();

        builder.Property(x => x.EventType)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .IsRequired();

        // Eski kayıtları temizlemek için index
        builder.HasIndex(x => x.ProcessedAt)
            .HasDatabaseName("ix_processed_events_processed_at");
    }
}
