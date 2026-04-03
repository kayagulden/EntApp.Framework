using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntApp.Shared.Infrastructure.Persistence.Outbox;

/// <summary>
/// OutboxMessage EF Core konfigürasyonu.
/// Her modülün DbContext'inde Outbox tablosu oluşturulur.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    private readonly string _schema;

    public OutboxMessageConfiguration(string schema = "app")
    {
        _schema = schema;
    }

    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("outbox_messages", _schema);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Type)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.Error)
            .HasMaxLength(2000);

        // İşlenmemiş mesajları hızlı bulmak için index
        builder.HasIndex(x => x.ProcessedAt)
            .HasFilter("processed_at IS NULL")
            .HasDatabaseName("ix_outbox_messages_unprocessed");
    }
}
