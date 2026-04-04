using EntApp.Modules.Workflow.Domain.Entities;
using EntApp.Modules.Workflow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Workflow.Infrastructure.Persistence;

/// <summary>
/// Workflow modülü DbContext — schema: wf
/// </summary>
public sealed class WorkflowDbContext : DbContext
{
    public const string Schema = "wf";

    public DbSet<WorkflowDefinition> Definitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowInstance> Instances => Set<WorkflowInstance>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();

    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        // ── WorkflowDefinition ───────────────────────────
        modelBuilder.Entity<WorkflowDefinition>(e =>
        {
            e.ToTable("workflow_definitions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Category).HasMaxLength(50);
            e.Property(x => x.StepDefinitionsJson).HasColumnType("jsonb");
            e.Property(x => x.ApprovalType)
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        // ── WorkflowInstance ─────────────────────────────
        modelBuilder.Entity<WorkflowInstance>(e =>
        {
            e.ToTable("workflow_instances");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => new { x.ReferenceType, x.ReferenceId });
            e.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
            e.Property(x => x.ReferenceType).HasMaxLength(100);
            e.Property(x => x.ReferenceId).HasMaxLength(100);
            e.Property(x => x.Metadata).HasColumnType("jsonb");

            e.HasOne(x => x.Definition)
                .WithMany(d => d.Instances)
                .HasForeignKey(x => x.DefinitionId);
        });

        // ── ApprovalStep ─────────────────────────────────
        modelBuilder.Entity<ApprovalStep>(e =>
        {
            e.ToTable("approval_steps");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.InstanceId, x.StepOrder });
            e.HasIndex(x => new { x.AssignedUserId, x.Status });
            e.Property(x => x.StepName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
            e.Property(x => x.AssignedRole).HasMaxLength(100);
            e.Property(x => x.Comment).HasMaxLength(2000);

            e.HasOne(x => x.Instance)
                .WithMany(i => i.Steps)
                .HasForeignKey(x => x.InstanceId);
        });
    }
}
