using EntApp.Modules.Audit.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Audit.Infrastructure.Persistence;

public class AuditDbContext : DbContext
{
    public const string Schema = "audit";

    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LoginRecord> LoginRecords => Set<LoginRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // ── AuditLog ────────────────────────────────
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs");
            e.HasKey(x => x.Id);

            e.Property(x => x.UserName).HasMaxLength(100);
            e.Property(x => x.Action).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(200);
            e.Property(x => x.EntityId).HasMaxLength(100);
            e.Property(x => x.IpAddress).HasMaxLength(50);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.Property(x => x.Description).HasMaxLength(2000);

            // JSON columns
            e.Property(x => x.OldValues).HasColumnType("jsonb");
            e.Property(x => x.NewValues).HasColumnType("jsonb");
            e.Property(x => x.AffectedColumns).HasColumnType("jsonb");

            // Indexes
            e.HasIndex(x => x.Timestamp);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.EntityType);
            e.HasIndex(x => x.Action);
            e.HasIndex(x => x.TenantId);
        });

        // ── LoginRecord ─────────────────────────────
        modelBuilder.Entity<LoginRecord>(e =>
        {
            e.ToTable("LoginRecords");
            e.HasKey(x => x.Id);

            e.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            e.Property(x => x.IpAddress).HasMaxLength(50);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.Property(x => x.Result).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.FailureReason).HasMaxLength(500);

            // Indexes
            e.HasIndex(x => x.Timestamp);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.Result);
            e.HasIndex(x => x.TenantId);
        });
    }
}
