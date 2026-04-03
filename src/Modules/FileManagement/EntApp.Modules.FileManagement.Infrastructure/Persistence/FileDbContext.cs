using EntApp.Modules.FileManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.FileManagement.Infrastructure.Persistence;

public class FileDbContext : DbContext
{
    public const string Schema = "file";

    public FileDbContext(DbContextOptions<FileDbContext> options) : base(options) { }

    public DbSet<FileEntry> Files => Set<FileEntry>();
    public DbSet<FileVersion> Versions => Set<FileVersion>();
    public DbSet<FileTag> Tags => Set<FileTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<FileEntry>(e =>
        {
            e.ToTable("Files");
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(500).IsRequired();
            e.Property(x => x.OriginalFileName).HasMaxLength(500).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
            e.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Category).HasMaxLength(100);

            e.HasMany(x => x.Versions).WithOne().HasForeignKey(v => v.FileEntryId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Tags).WithOne().HasForeignKey(t => t.FileEntryId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.Category);
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => x.IsDeleted);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<FileVersion>(e =>
        {
            e.ToTable("Versions");
            e.HasKey(x => x.Id);
            e.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
            e.Property(x => x.ChangeNote).HasMaxLength(500);
            e.HasIndex(x => new { x.FileEntryId, x.VersionNumber }).IsUnique();
        });

        modelBuilder.Entity<FileTag>(e =>
        {
            e.ToTable("Tags");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.FileEntryId, x.Name }).IsUnique();
            e.HasIndex(x => x.Name);
        });
    }
}
