using EntApp.Modules.TaskManagement.Domain.Entities;
using EntApp.Modules.TaskManagement.Domain.Ids;
using EntApp.Shared.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using TaskStatusEnum = EntApp.Modules.TaskManagement.Domain.Enums.TaskStatus;

namespace EntApp.Modules.TaskManagement.Infrastructure.Persistence;

/// <summary>TaskManagement modülü DbContext — schema: pm</summary>
public sealed class TaskManagementDbContext : BaseDbContext
{
    public const string Schema = "pm";
    protected override string SchemaName => Schema;

    public DbSet<PortfolioBase> Portfolios => Set<PortfolioBase>();
    public DbSet<ProjectBase> Projects => Set<ProjectBase>();
    public DbSet<ApplicationBase> Applications => Set<ApplicationBase>();
    public DbSet<TaskItemBase> Tasks => Set<TaskItemBase>();
    public DbSet<CommentBase> Comments => Set<CommentBase>();
    public DbSet<TimeEntryBase> TimeEntries => Set<TimeEntryBase>();

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

        // ── Task ───────────────────────────────────────────
        modelBuilder.Entity<TaskItemBase>(e =>
        {
            e.ToTable("tasks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<TaskItemId>());
            e.Property(x => x.ProjectId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new ProjectId(v.Value) : null).IsRequired(false);
            e.Property(x => x.ParentTaskId).HasConversion(new StronglyTypedIdValueConverter<TaskItemId>());
            e.HasIndex(x => x.TaskNumber).IsUnique();
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.AssigneeUserId);
            e.Property(x => x.TaskNumber).HasMaxLength(20).IsRequired();
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Description).HasMaxLength(5000);
            e.Property(x => x.Tags).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.EstimatedHours).HasPrecision(8, 2);

            // Source referansı (cross-module)
            e.Property(x => x.SourceModule).HasMaxLength(50);
            e.Property(x => x.SourceType).HasMaxLength(50);
            e.HasIndex(x => new { x.SourceModule, x.SourceType, x.SourceId })
                .HasFilter("source_id IS NOT NULL")
                .HasDatabaseName("ix_tasks_source");

            e.HasOne(x => x.Project).WithMany(p => p.Tasks).HasForeignKey(x => x.ProjectId).IsRequired(false);
            e.HasOne(x => x.ParentTask).WithMany(t => t.SubTasks).HasForeignKey(x => x.ParentTaskId);
            e.Ignore(x => x.TotalLoggedHours);
        });

        // ── Comment ────────────────────────────────────────
        modelBuilder.Entity<CommentBase>(e =>
        {
            e.ToTable("comments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<CommentId>());
            e.Property(x => x.TaskId).HasConversion(new StronglyTypedIdValueConverter<TaskItemId>());
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
            e.Property(x => x.TaskId).HasConversion(new StronglyTypedIdValueConverter<TaskItemId>());
            e.HasIndex(x => x.TaskId);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Hours).HasPrecision(8, 2);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasOne(x => x.Task).WithMany(t => t.TimeEntries).HasForeignKey(x => x.TaskId);
        });
    }
}
