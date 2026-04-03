using EntApp.Modules.Configuration.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Configuration.Infrastructure.Persistence;

public class ConfigDbContext : DbContext
{
    public const string Schema = "config";

    public ConfigDbContext(DbContextOptions<ConfigDbContext> options) : base(options) { }

    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<AppSetting>(e =>
        {
            e.ToTable("AppSettings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(200).IsRequired();
            e.Property(x => x.Value).IsRequired();
            e.Property(x => x.ValueType).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Group).HasMaxLength(100);

            // Key + TenantId unique
            e.HasIndex(x => new { x.Key, x.TenantId }).IsUnique();
            e.HasIndex(x => x.Group);
        });

        modelBuilder.Entity<FeatureFlag>(e =>
        {
            e.ToTable("FeatureFlags");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(300).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.AllowedRoles).HasColumnType("jsonb");
            e.Property(x => x.Metadata).HasColumnType("jsonb");

            // Name + TenantId unique
            e.HasIndex(x => new { x.Name, x.TenantId }).IsUnique();
        });
    }
}
