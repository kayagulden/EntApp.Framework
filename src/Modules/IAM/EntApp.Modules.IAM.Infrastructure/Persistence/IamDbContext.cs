using EntApp.Modules.IAM.Domain.Entities;
using EntApp.Shared.Kernel.Domain.Entities;
using EntApp.Shared.Kernel.Domain.Ids;
using EntApp.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.IAM.Infrastructure.Persistence;

/// <summary>
/// IAM modülü EF Core DbContext.
/// Kendi şeması: "iam"
/// Organization ve Department artık Shared Kernel'da (org şeması).
/// User entity cross-schema FK ile org.organizations/org.departments'a referans verir.
/// </summary>
public sealed class IamDbContext : DbContext
{
    public const string Schema = "iam";

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public IamDbContext(DbContextOptions<IamDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);

        // ── Organization & Department (org schema — read-only mapping for navigation) ──
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("organizations", OrganizationDbContext.Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasConversion(
                v => v.Value, v => new OrganizationId(v));
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Code).HasMaxLength(20);

            entity.Property(e => e.ParentId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new OrganizationId(v.Value) : null);

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("departments", OrganizationDbContext.Schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasConversion(
                v => v.Value, v => new DepartmentId(v));
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.Property(e => e.OrganizationId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new OrganizationId(v.Value) : null);

            entity.Property(e => e.ParentDepartmentId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new DepartmentId(v.Value) : null);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Departments)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.ParentDepartment)
                .WithMany(e => e.SubDepartments)
                .HasForeignKey(e => e.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── User ────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KeycloakId).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.KeycloakId).IsUnique();
            entity.Property(e => e.UserName).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Ignore(e => e.FullName);

            // Strongly typed ID conversion for FK properties
            entity.Property(e => e.OrganizationId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new OrganizationId(v.Value) : null);

            entity.Property(e => e.DepartmentId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new DepartmentId(v.Value) : null);

            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Role ────────────────────────────────────────────
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

        });

        // ── Permission ──────────────────────────────────────
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SystemName).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.SystemName).IsUnique();
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Module).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // ── UserRole (join) ─────────────────────────────────
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.HasOne(e => e.User).WithMany(u => u.UserRoles).HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Role).WithMany(r => r.UserRoles).HasForeignKey(e => e.RoleId);
        });

        // ── RolePermission (join) ───────────────────────────
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(e => new { e.RoleId, e.PermissionId });
            entity.HasOne(e => e.Role).WithMany(r => r.RolePermissions).HasForeignKey(e => e.RoleId);
            entity.HasOne(e => e.Permission).WithMany().HasForeignKey(e => e.PermissionId);
        });
    }
}
