using EntApp.Modules.MultiTenancy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.MultiTenancy.Infrastructure.Persistence;

public class TenantDbContext : DbContext
{
    public const string Schema = "tenant";

    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantSetting> Settings => Set<TenantSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("Tenants");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Identifier).HasMaxLength(50).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Subdomain).HasMaxLength(100);
            e.Property(x => x.ConnectionString).HasMaxLength(1000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Plan).HasMaxLength(50).IsRequired();
            e.Property(x => x.AdminEmail).HasMaxLength(300);
            e.Property(x => x.LogoUrl).HasMaxLength(1000);

            e.HasMany(x => x.Settings).WithOne().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.Identifier).IsUnique();
            e.HasIndex(x => x.Subdomain).IsUnique().HasFilter("\"Subdomain\" IS NOT NULL");
            e.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<TenantSetting>(e =>
        {
            e.ToTable("Settings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(200).IsRequired();
            e.Property(x => x.Value).HasMaxLength(4000).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.Key }).IsUnique();
        });
    }
}
