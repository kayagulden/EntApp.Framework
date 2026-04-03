using EntApp.Modules.IAM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.IAM.Infrastructure.Persistence;

/// <summary>
/// IAM modülü EF Core DbContext.
/// Kendi şeması: "iam"
/// </summary>
public sealed class IamDbContext : DbContext
{
    public const string Schema = "iam";

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public IamDbContext(DbContextOptions<IamDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);

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
            entity.Ignore(e => e.DomainEvents);

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
            entity.Ignore(e => e.DomainEvents);
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

        // ── Organization ────────────────────────────────────
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("organizations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Ignore(e => e.DomainEvents);

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
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
            entity.Ignore(e => e.DomainEvents);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Departments)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
