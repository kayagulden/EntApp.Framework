using EntApp.Modules.AI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.AI.Infrastructure.Persistence;

/// <summary>
/// AI modülü DbContext — ai schema'sında.
/// </summary>
public sealed class AiDbContext : DbContext
{
    public const string Schema = "ai";

    public DbSet<AiModel> AiModels => Set<AiModel>();
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();
    public DbSet<EmbeddingDocument> EmbeddingDocuments => Set<EmbeddingDocument>();
    public DbSet<AiUsageLog> AiUsageLogs => Set<AiUsageLog>();

    public AiDbContext(DbContextOptions<AiDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        // ── AiModel ─────────────────────────────────────────
        modelBuilder.Entity<AiModel>(b =>
        {
            b.ToTable("ai_models");
            b.HasIndex(e => new { e.TenantId, e.Provider, e.ModelName })
                .IsUnique()
                .HasFilter("is_deleted = false");
            b.Property(e => e.ModelName).HasMaxLength(100).IsRequired();
            b.Property(e => e.DisplayName).HasMaxLength(150).IsRequired();
            b.HasQueryFilter(e => !e.IsDeleted);
        });

        // ── PromptTemplate ──────────────────────────────────
        modelBuilder.Entity<PromptTemplate>(b =>
        {
            b.ToTable("prompt_templates");
            b.HasIndex(e => new { e.TenantId, e.Key, e.Version })
                .IsUnique()
                .HasFilter("is_deleted = false");
            b.Property(e => e.Key).HasMaxLength(100).IsRequired();
            b.Property(e => e.Title).HasMaxLength(200).IsRequired();
            b.Property(e => e.TemplateContent).HasMaxLength(10000).IsRequired();
            b.Property(e => e.Category).HasMaxLength(50);
            b.HasQueryFilter(e => !e.IsDeleted);
        });

        // ── EmbeddingDocument ───────────────────────────────
        modelBuilder.Entity<EmbeddingDocument>(b =>
        {
            b.ToTable("embedding_documents");
            b.HasIndex(e => new { e.TenantId, e.ModuleName, e.SourceType, e.SourceId });
            b.Property(e => e.ModuleName).HasMaxLength(50).IsRequired();
            b.Property(e => e.SourceType).HasMaxLength(50).IsRequired();
            b.Property(e => e.SourceId).HasMaxLength(100);
            b.Property(e => e.Content).IsRequired();
            b.HasQueryFilter(e => !e.IsDeleted);
        });

        // ── AiUsageLog ──────────────────────────────────────
        modelBuilder.Entity<AiUsageLog>(b =>
        {
            b.ToTable("ai_usage_logs");
            b.HasIndex(e => new { e.TenantId, e.CreatedAt });
            b.HasIndex(e => new { e.TenantId, e.Provider, e.Operation });
            b.Property(e => e.ModelName).HasMaxLength(100).IsRequired();
            b.Property(e => e.ErrorMessage).HasMaxLength(2000);
            b.Property(e => e.ModuleName).HasMaxLength(50);
            b.Property(e => e.EstimatedCost).HasPrecision(18, 8);
            b.HasQueryFilter(e => !e.IsDeleted);
        });
    }
}
