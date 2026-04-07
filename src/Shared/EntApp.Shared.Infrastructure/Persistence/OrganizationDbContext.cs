using EntApp.Shared.Contracts.Identity;
using EntApp.Shared.Kernel.Domain.Entities;
using EntApp.Shared.Kernel.Domain.Ids;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Shared.Infrastructure.Persistence;

/// <summary>
/// Organization ve Department entity'lerini yöneten paylaşılan DbContext.
/// Schema: "org" — tüm modüller bu tablolara FK atabilir.
/// </summary>
public sealed class OrganizationDbContext : BaseDbContext
{
    public const string Schema = "org";
    protected override string SchemaName => Schema;

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Department> Departments => Set<Department>();

    public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options)
        : base(options) { }

    public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options, ICurrentTenant? currentTenant)
        : base(options, currentTenant) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Organization ────────────────────────────────────
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("organizations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Department ──────────────────────────────────────
        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("departments");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.HasIndex(e => e.Name);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Departments)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ParentDepartment)
                .WithMany(e => e.SubDepartments)
                .HasForeignKey(e => e.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
