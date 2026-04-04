using EntApp.Modules.TaskManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using TaskStatusEnum = EntApp.Modules.TaskManagement.Domain.Enums.TaskStatus;

namespace EntApp.Modules.TaskManagement.Infrastructure.Persistence;

/// <summary>TaskManagement modülü DbContext — schema: pm</summary>
public sealed class TaskManagementDbContext : DbContext
{
    public const string Schema = "pm";

    public DbSet<ProjectBase> Projects => Set<ProjectBase>();
    public DbSet<TaskItemBase> Tasks => Set<TaskItemBase>();
    public DbSet<CommentBase> Comments => Set<CommentBase>();
    public DbSet<TimeEntryBase> TimeEntries => Set<TimeEntryBase>();

    public TaskManagementDbContext(DbContextOptions<TaskManagementDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<ProjectBase>(e =>
        {
            e.ToTable("projects");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.Key).HasMaxLength(10).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<TaskItemBase>(e =>
        {
            e.ToTable("tasks");
            e.HasKey(x => x.Id);
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
            e.HasOne(x => x.Project).WithMany(p => p.Tasks).HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.ParentTask).WithMany(t => t.SubTasks).HasForeignKey(x => x.ParentTaskId);
            e.Ignore(x => x.TotalLoggedHours);
        });

        modelBuilder.Entity<CommentBase>(e =>
        {
            e.ToTable("comments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TaskId);
            e.Property(x => x.Content).HasMaxLength(5000).IsRequired();
            e.HasOne(x => x.Task).WithMany(t => t.Comments).HasForeignKey(x => x.TaskId);
        });

        modelBuilder.Entity<TimeEntryBase>(e =>
        {
            e.ToTable("time_entries");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TaskId);
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Hours).HasPrecision(8, 2);
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasOne(x => x.Task).WithMany(t => t.TimeEntries).HasForeignKey(x => x.TaskId);
        });
    }
}
