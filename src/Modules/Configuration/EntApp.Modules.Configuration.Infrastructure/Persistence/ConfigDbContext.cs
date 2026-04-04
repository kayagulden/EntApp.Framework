using EntApp.Modules.Configuration.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Configuration.Infrastructure.Persistence;

public class ConfigDbContext : DbContext
{
    public const string Schema = "config";

    public ConfigDbContext(DbContextOptions<ConfigDbContext> options) : base(options) { }

    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Currency> Currencies => Set<Currency>();

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

        // ── Dynamic Entity: Country ─────────────────────────
        modelBuilder.Entity<Country>(e =>
        {
            e.ToTable("Countries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(3).IsRequired();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.PhoneCode).HasMaxLength(5);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasIndex(x => x.Code).IsUnique();
        });

        // ── Dynamic Entity: City ────────────────────────────
        modelBuilder.Entity<City>(e =>
        {
            e.ToTable("Cities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.PlateCode).HasMaxLength(10);
            e.HasOne(x => x.Country)
                .WithMany()
                .HasForeignKey(x => x.CountryId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasIndex(x => new { x.Name, x.CountryId }).IsUnique();
        });

        // ── Dynamic Entity: Currency ────────────────────────
        modelBuilder.Entity<Currency>(e =>
        {
            e.ToTable("Currencies");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(3).IsRequired();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Symbol).HasMaxLength(5);
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasIndex(x => x.Code).IsUnique();
        });
    }
}

