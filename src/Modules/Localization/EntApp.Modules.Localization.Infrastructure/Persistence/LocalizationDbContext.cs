using EntApp.Modules.Localization.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Localization.Infrastructure.Persistence;

public class LocalizationDbContext : DbContext
{
    public const string Schema = "localization";

    public LocalizationDbContext(DbContextOptions<LocalizationDbContext> options) : base(options) { }

    public DbSet<Language> Languages => Set<Language>();
    public DbSet<TranslationEntry> Translations => Set<TranslationEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Language>(e =>
        {
            e.ToTable("Languages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(10).IsRequired();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.NativeName).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<TranslationEntry>(e =>
        {
            e.ToTable("Translations");
            e.HasKey(x => x.Id);
            e.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.Namespace).HasMaxLength(100).IsRequired();
            e.Property(x => x.Key).HasMaxLength(500).IsRequired();
            e.Property(x => x.Value).HasMaxLength(10000).IsRequired();
            e.Property(x => x.LastModifiedBy).HasMaxLength(200);
            e.HasIndex(x => new { x.LanguageCode, x.Namespace, x.Key, x.TenantId }).IsUnique();
            e.HasIndex(x => x.LanguageCode);
            e.HasIndex(x => x.Namespace);
        });
    }
}
