using EntApp.Modules.KnowledgeBase.Domain.Entities;
using EntApp.Modules.KnowledgeBase.Domain.Ids;
using EntApp.Shared.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.KnowledgeBase.Infrastructure.Persistence;

public sealed class KnowledgeBaseDbContext : BaseDbContext
{
    public const string Schema = "kb";

    protected override string SchemaName => Schema;

    public DbSet<WikiSpace> WikiSpaces => Set<WikiSpace>();
    public DbSet<WikiPage> WikiPages => Set<WikiPage>();
    public DbSet<WikiPageVersion> WikiPageVersions => Set<WikiPageVersion>();

    public KnowledgeBaseDbContext(DbContextOptions<KnowledgeBaseDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Tenant + Soft Delete Filters ──────────────────────────
        ApplyTenantFilter<WikiSpace, WikiSpaceId>(modelBuilder);
        ApplyTenantFilter<WikiPage, WikiPageId>(modelBuilder);
        ApplyTenantFilter<WikiPageVersion, WikiPageVersionId>(modelBuilder);

        // ── WikiSpace ─────────────────────────────────────────────
        modelBuilder.Entity<WikiSpace>(e =>
        {
            e.ToTable("wiki_spaces");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.IconEmoji).HasMaxLength(10);

            // Slug tenant içinde unique (soft delete hariç)
            e.HasIndex(x => new { x.TenantId, x.Slug })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("ix_wiki_spaces_tenant_slug");

            e.HasIndex(x => x.ProjectId)
                .HasDatabaseName("ix_wiki_spaces_project");
        });

        // ── WikiPage ──────────────────────────────────────────────
        modelBuilder.Entity<WikiPage>(e =>
        {
            e.ToTable("wiki_pages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            e.Property(x => x.ContentJson).HasColumnType("text").IsRequired();
            e.Property(x => x.ContentHtml).HasColumnType("text").IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();

            // Slug space içinde unique (soft delete hariç)
            e.HasIndex(x => new { x.WikiSpaceId, x.Slug })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false")
                .HasDatabaseName("ix_wiki_pages_space_slug");

            e.HasIndex(x => x.ParentPageId)
                .HasDatabaseName("ix_wiki_pages_parent");

            e.HasIndex(x => x.Status)
                .HasDatabaseName("ix_wiki_pages_status");

            e.HasIndex(x => x.SourceRequirementId)
                .HasFilter("\"SourceRequirementId\" IS NOT NULL")
                .HasDatabaseName("ix_wiki_pages_source_requirement");

            e.HasIndex(x => x.SourceTicketId)
                .HasFilter("\"SourceTicketId\" IS NOT NULL")
                .HasDatabaseName("ix_wiki_pages_source_ticket");

            // Nullable FK — ParentPage
            e.Property(x => x.ParentPageId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new WikiPageId(v.Value) : null).IsRequired(false);

            e.HasOne(x => x.Space)
                .WithMany(s => s.Pages)
                .HasForeignKey(x => x.WikiSpaceId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.ParentPage)
                .WithMany(p => p.ChildPages)
                .HasForeignKey(x => x.ParentPageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── WikiPageVersion ───────────────────────────────────────
        modelBuilder.Entity<WikiPageVersion>(e =>
        {
            e.ToTable("wiki_page_versions");
            e.HasKey(x => x.Id);
            e.Property(x => x.ContentJson).HasColumnType("text").IsRequired();
            e.Property(x => x.ContentHtml).HasColumnType("text").IsRequired();
            e.Property(x => x.ChangeNote).HasMaxLength(500);

            e.HasOne(x => x.Page)
                .WithMany(p => p.Versions)
                .HasForeignKey(x => x.WikiPageId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.WikiPageId, x.VersionNumber })
                .IsUnique()
                .HasDatabaseName("ix_wiki_page_versions_page_number");
        });
    }
}
