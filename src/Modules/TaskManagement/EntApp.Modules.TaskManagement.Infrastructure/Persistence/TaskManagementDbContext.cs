using EntApp.Modules.TaskManagement.Domain.Entities;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Modules.TaskManagement.Domain.Enums;
using EntApp.Shared.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using WorkItemStatusEnum = EntApp.Modules.TaskManagement.Domain.Enums.WorkItemStatus;

namespace EntApp.Modules.TaskManagement.Infrastructure.Persistence;

/// <summary>TaskManagement modülü DbContext — schema: pm</summary>
public sealed class TaskManagementDbContext : BaseDbContext
{
    public const string Schema = "pm";
    protected override string SchemaName => Schema;

    public DbSet<PortfolioBase> Portfolios => Set<PortfolioBase>();
    public DbSet<ProjectBase> Projects => Set<ProjectBase>();
    public DbSet<ApplicationBase> Applications => Set<ApplicationBase>();
    public DbSet<WorkItemBase> WorkItems => Set<WorkItemBase>();
    public DbSet<CommentBase> Comments => Set<CommentBase>();
    public DbSet<TimeEntryBase> TimeEntries => Set<TimeEntryBase>();
    public DbSet<ProjectDeliverable> ProjectDeliverables => Set<ProjectDeliverable>();
    public DbSet<ServerCI> Servers => Set<ServerCI>();
    public DbSet<DatabaseCI> Databases => Set<DatabaseCI>();
    public DbSet<LicenceCI> Licences => Set<LicenceCI>();
    public DbSet<CIRelationship> CIRelationships => Set<CIRelationship>();
    public DbSet<SprintBase> Sprints => Set<SprintBase>();

    public TaskManagementDbContext(DbContextOptions<TaskManagementDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Portfolio ──────────────────────────────────────
        modelBuilder.Entity<PortfolioBase>(e =>
        {
            e.ToTable("portfolios");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<PortfolioId>());
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Description).HasMaxLength(5000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        // ── Project ────────────────────────────────────────
        modelBuilder.Entity<ProjectBase>(e =>
        {
            e.ToTable("projects");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<ProjectId>());
            e.Property(x => x.PortfolioId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new PortfolioId(v.Value) : null).IsRequired(false);
            e.HasIndex(x => x.Key).IsUnique();
            e.HasIndex(x => x.PortfolioId);
            e.Property(x => x.Key).HasMaxLength(10).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Methodology).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(30);
            e.HasOne(x => x.Portfolio).WithMany(p => p.Projects).HasForeignKey(x => x.PortfolioId).IsRequired(false);
        });

        // ── Configuration Item (CMDB Base — TPT) ─────────────
        modelBuilder.Entity<ConfigurationItemBase>(e =>
        {
            e.ToTable("configuration_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<ConfigurationItemId>());
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Criticality).HasConversion<string>().HasMaxLength(20);
        });

        // ── Application (CI derived — TPT) ────────────────
        modelBuilder.Entity<ApplicationBase>(e =>
        {
            e.ToTable("applications");
            e.Property(x => x.ApplicationType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.TechnologyStack).HasMaxLength(500);
            e.Property(x => x.RepositoryUrl).HasMaxLength(500);
            e.Property(x => x.DocumentationUrl).HasMaxLength(500);
            e.Property(x => x.CurrentVersion).HasMaxLength(50);
        });

        // ── Server (CI derived — TPT) ─────────────────────
        modelBuilder.Entity<ServerCI>(e =>
        {
            e.ToTable("servers");
            e.Property(x => x.ServerType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Environment).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.OperatingSystem).HasMaxLength(100);
            e.Property(x => x.IpAddress).HasMaxLength(100);
            e.Property(x => x.Hostname).HasMaxLength(200);
            e.Property(x => x.DataCenter).HasMaxLength(200);
        });

        // ── Database (CI derived — TPT) ───────────────────
        modelBuilder.Entity<DatabaseCI>(e =>
        {
            e.ToTable("databases");
            e.Property(x => x.DatabaseEngine).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Version).HasMaxLength(50);
            e.Property(x => x.ConnectionString).HasMaxLength(500);
            e.Property(x => x.BackupSchedule).HasMaxLength(200);
            e.Property(x => x.SizeGB).HasPrecision(10, 2);
        });

        // ── Licence (CI derived — TPT) ────────────────────
        modelBuilder.Entity<LicenceCI>(e =>
        {
            e.ToTable("licences");
            e.Property(x => x.LicenceType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Vendor).HasMaxLength(200);
            e.Property(x => x.ProductName).HasMaxLength(200);
            e.Property(x => x.LicenceKey).HasMaxLength(500);
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.AnnualCost).HasPrecision(18, 2);
        });

        // ── Sprint ─────────────────────────────────────────
        modelBuilder.Entity<SprintBase>(e =>
        {
            e.ToTable("sprints");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<SprintId>());
            e.Property(x => x.ProjectId).HasConversion(new StronglyTypedIdValueConverter<ProjectId>());
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.Status);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Goal).HasMaxLength(2000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
        });

        // ── Task ───────────────────────────────────────────
        modelBuilder.Entity<WorkItemBase>(e =>
        {
            e.ToTable("work_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<WorkItemId>());
            e.Property(x => x.ProjectId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new ProjectId(v.Value) : null).IsRequired(false);
            e.Property(x => x.ParentTaskId).HasConversion(new StronglyTypedIdValueConverter<WorkItemId>());
            e.HasIndex(x => x.WorkItemNumber).IsUnique();
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.AssigneeUserId);
            e.Property(x => x.WorkItemNumber).HasMaxLength(20).IsRequired();
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Description).HasMaxLength(5000);
            e.Property(x => x.Tags).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.EstimatedHours).HasPrecision(8, 2);

            // Work Item hierarchy & Sprint alanları
            e.Property(x => x.AcceptanceCriteria).HasColumnType("text");
            e.Property(x => x.SprintId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new SprintId(v.Value) : null).IsRequired(false);
            e.HasIndex(x => x.SprintId).HasDatabaseName("ix_tasks_sprint");
            e.Property(x => x.HierarchyLevel).HasDefaultValue(0);

            // Source referansı (cross-module)
            e.Property(x => x.SourceModule).HasMaxLength(50);
            e.Property(x => x.SourceType).HasMaxLength(50);
            e.HasIndex(x => new { x.SourceModule, x.SourceType, x.SourceId })
                .HasFilter("source_id IS NOT NULL")
                .HasDatabaseName("ix_tasks_source");

            e.HasOne(x => x.Project).WithMany(p => p.WorkItems).HasForeignKey(x => x.ProjectId).IsRequired(false);
            e.HasOne(x => x.ParentTask).WithMany(t => t.SubTasks).HasForeignKey(x => x.ParentTaskId);
            e.HasOne(x => x.Sprint).WithMany(s => s.WorkItems).HasForeignKey(x => x.SprintId).IsRequired(false);
            e.Ignore(x => x.TotalLoggedHours);
        });

        // ── Comment ────────────────────────────────────────
        modelBuilder.Entity<CommentBase>(e =>
        {
            e.ToTable("comments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<CommentId>());
            e.Property(x => x.TaskId).HasConversion(new StronglyTypedIdValueConverter<WorkItemId>());
            e.HasIndex(x => x.TaskId);
            e.Property(x => x.Content).HasMaxLength(5000).IsRequired();
            e.HasOne(x => x.Task).WithMany(t => t.Comments).HasForeignKey(x => x.TaskId);
        });

        // ── TimeEntry ──────────────────────────────────────
        modelBuilder.Entity<TimeEntryBase>(e =>
        {
            e.ToTable("time_entries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<TimeEntryId>());
            e.Property(x => x.TaskId).HasConversion(new StronglyTypedIdValueConverter<WorkItemId>());
            e.HasIndex(x => x.TaskId);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Hours).HasPrecision(8, 2);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasOne(x => x.Task).WithMany(t => t.TimeEntries).HasForeignKey(x => x.TaskId);
        });

        // ── ProjectDeliverable (Proje ↔ CI M:N) ────────────
        modelBuilder.Entity<ProjectDeliverable>(e =>
        {
            e.ToTable("project_deliverables");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<ProjectDeliverableId>());
            e.Property(x => x.ProjectId).HasConversion(new StronglyTypedIdValueConverter<ProjectId>());
            e.Property(x => x.ConfigurationItemId).HasConversion(new StronglyTypedIdValueConverter<ConfigurationItemId>());
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasIndex(x => new { x.ProjectId, x.ConfigurationItemId }).IsUnique()
                .HasDatabaseName("ix_project_deliverables_unique");
            e.HasOne(x => x.Project).WithMany(p => p.Deliverables).HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.ConfigurationItem).WithMany(ci => ci.ProjectDeliverables).HasForeignKey(x => x.ConfigurationItemId);
        });

        // ── CIRelationship (CI ↔ CI directed graph) ─────────
        modelBuilder.Entity<CIRelationship>(e =>
        {
            e.ToTable("ci_relationships");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<CIRelationshipId>());
            e.Property(x => x.SourceCIId).HasConversion(new StronglyTypedIdValueConverter<ConfigurationItemId>());
            e.Property(x => x.TargetCIId).HasConversion(new StronglyTypedIdValueConverter<ConfigurationItemId>());
            e.Property(x => x.RelationType).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasIndex(x => new { x.SourceCIId, x.TargetCIId, x.RelationType }).IsUnique()
                .HasDatabaseName("ix_ci_relationships_unique");
            e.HasIndex(x => x.SourceCIId).HasDatabaseName("ix_ci_relationships_source");
            e.HasIndex(x => x.TargetCIId).HasDatabaseName("ix_ci_relationships_target");
            e.HasOne(x => x.SourceCI).WithMany().HasForeignKey(x => x.SourceCIId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TargetCI).WithMany().HasForeignKey(x => x.TargetCIId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
