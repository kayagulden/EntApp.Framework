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
    public DbSet<BoardColumn> BoardColumns => Set<BoardColumn>();
    public DbSet<BurndownSnapshot> BurndownSnapshots => Set<BurndownSnapshot>();
    public DbSet<MilestoneBase> Milestones => Set<MilestoneBase>();
    public DbSet<ProjectTemplate> ProjectTemplates => Set<ProjectTemplate>();
    public DbSet<Requirement> Requirements => Set<Requirement>();
    public DbSet<TestScenario> TestScenarios => Set<TestScenario>();
    public DbSet<TestStep> TestSteps => Set<TestStep>();
    public DbSet<TestPlan> TestPlans => Set<TestPlan>();
    public DbSet<TestPlanScenario> TestPlanScenarios => Set<TestPlanScenario>();
    public DbSet<TestExecution> TestExecutions => Set<TestExecution>();
    public DbSet<TestStepResult> TestStepResults => Set<TestStepResult>();
    public DbSet<Release> Releases => Set<Release>();
    public DbSet<ReleaseItem> ReleaseItems => Set<ReleaseItem>();
    public DbSet<GoNoGoChecklist> GoNoGoChecklists => Set<GoNoGoChecklist>();
    public DbSet<GoNoGoItem> GoNoGoItems => Set<GoNoGoItem>();
    public DbSet<ReleaseNote> ReleaseNotes => Set<ReleaseNote>();

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
            e.Property(x => x.MilestoneId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new MilestoneId(v.Value) : null).IsRequired(false);
            e.HasOne(x => x.Milestone).WithMany(m => m.Sprints).HasForeignKey(x => x.MilestoneId).IsRequired(false);
        });

        // ── BoardColumn ──────────────────────────────────────
        modelBuilder.Entity<BoardColumn>(e =>
        {
            e.ToTable("board_columns");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<BoardColumnId>());
            e.Property(x => x.ProjectId).HasConversion(new StronglyTypedIdValueConverter<ProjectId>());
            e.HasIndex(x => x.ProjectId);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.MappedStatus).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
        });

        // ── BurndownSnapshot ─────────────────────────────────
        modelBuilder.Entity<BurndownSnapshot>(e =>
        {
            e.ToTable("burndown_snapshots");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<BurndownSnapshotId>());
            e.Property(x => x.SprintId).HasConversion(new StronglyTypedIdValueConverter<SprintId>());
            e.HasIndex(x => x.SprintId);
            e.HasIndex(x => new { x.SprintId, x.Date }).IsUnique();
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
            e.Property(x => x.MilestoneId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new MilestoneId(v.Value) : null).IsRequired(false);
            e.HasIndex(x => x.MilestoneId).HasDatabaseName("ix_tasks_milestone");
            e.HasOne(x => x.Milestone).WithMany(m => m.WorkItems).HasForeignKey(x => x.MilestoneId).IsRequired(false);
            e.Property(x => x.RequirementId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new RequirementId(v.Value) : null).IsRequired(false);
            e.HasIndex(x => x.RequirementId).HasDatabaseName("ix_tasks_requirement");
            e.HasOne(x => x.Requirement).WithMany(r => r.WorkItems).HasForeignKey(x => x.RequirementId).IsRequired(false);
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

        // ── Milestone ─────────────────────────────────────────
        modelBuilder.Entity<MilestoneBase>(e =>
        {
            e.ToTable("milestones");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<MilestoneId>());
            e.Property(x => x.ProjectId).HasConversion(new StronglyTypedIdValueConverter<ProjectId>());
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.DueDate);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
        });

        // ── ProjectTemplate ─────────────────────────────────────
        modelBuilder.Entity<ProjectTemplate>(e =>
        {
            e.ToTable("project_templates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<ProjectTemplateId>());
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Icon).HasMaxLength(10);
            e.Property(x => x.Methodology).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.EstimationMode).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.BoardColumnsJson).HasColumnType("text").IsRequired();
            e.Property(x => x.MilestonesJson).HasColumnType("text");
            e.Property(x => x.WorkItemsJson).HasColumnType("text");
            e.HasIndex(x => x.Name);
        });

        // ── Requirement ───────────────────────────────────────────
        modelBuilder.Entity<Requirement>(e =>
        {
            e.ToTable("requirements");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<RequirementId>());
            e.Property(x => x.ProjectId).HasConversion(new StronglyTypedIdValueConverter<ProjectId>());
            e.Property(x => x.ParentRequirementId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new RequirementId(v.Value) : null).IsRequired(false);
            e.Property(x => x.Key).HasMaxLength(30).IsRequired();
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Description).HasColumnType("text");
            e.Property(x => x.AcceptanceCriteria).HasColumnType("text");
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.SourceTicketNumber).HasMaxLength(20);
            e.Property(x => x.ExternalDesignUrl).HasMaxLength(500);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ParentRequirement).WithMany(r => r.Children).HasForeignKey(x => x.ParentRequirementId).IsRequired(false);
            e.HasIndex(x => new { x.ProjectId, x.Key }).IsUnique().HasDatabaseName("ix_requirements_project_key");
        });

        // ── TestScenario ─────────────────────────────────────────
        modelBuilder.Entity<TestScenario>(e =>
        {
            e.ToTable("test_scenarios");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<TestScenarioId>());
            e.Property(x => x.ProjectId).HasConversion(new StronglyTypedIdValueConverter<ProjectId>());
            e.Property(x => x.RequirementId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new RequirementId(v.Value) : null).IsRequired(false);
            e.Property(x => x.Key).HasMaxLength(30).IsRequired();
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Description).HasColumnType("text");
            e.Property(x => x.Preconditions).HasColumnType("text");
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Tags).HasMaxLength(500);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Requirement).WithMany().HasForeignKey(x => x.RequirementId).IsRequired(false);
            e.HasIndex(x => new { x.ProjectId, x.Key }).IsUnique().HasDatabaseName("ix_test_scenarios_project_key");
            e.HasIndex(x => x.RequirementId).HasDatabaseName("ix_test_scenarios_requirement");
        });

        // ── TestStep ─────────────────────────────────────────────
        modelBuilder.Entity<TestStep>(e =>
        {
            e.ToTable("test_steps");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<TestStepId>());
            e.Property(x => x.TestScenarioId).HasConversion(new StronglyTypedIdValueConverter<TestScenarioId>());
            e.Property(x => x.Action).HasMaxLength(1000).IsRequired();
            e.Property(x => x.ExpectedResult).HasMaxLength(1000).IsRequired();
            e.Property(x => x.TestData).HasMaxLength(2000);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasOne(x => x.TestScenario).WithMany(s => s.Steps).HasForeignKey(x => x.TestScenarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.TestScenarioId, x.StepNumber }).HasDatabaseName("ix_test_steps_scenario_number");
        });

        // ── TestPlan ─────────────────────────────────────────────
        modelBuilder.Entity<TestPlan>(e =>
        {
            e.ToTable("test_plans");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<TestPlanId>());
            e.Property(x => x.ProjectId).HasConversion(new StronglyTypedIdValueConverter<ProjectId>());
            e.Property(x => x.SprintId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new SprintId(v.Value) : null).IsRequired(false);
            e.Property(x => x.MilestoneId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new MilestoneId(v.Value) : null).IsRequired(false);
            e.Property(x => x.Key).HasMaxLength(30).IsRequired();
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Description).HasColumnType("text");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.AssignedTesterId).HasMaxLength(100);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ProjectId, x.Key }).IsUnique().HasDatabaseName("ix_test_plans_project_key");
        });

        // ── TestPlanScenario ─────────────────────────────────────
        modelBuilder.Entity<TestPlanScenario>(e =>
        {
            e.ToTable("test_plan_scenarios");
            e.HasKey(x => x.Id);
            e.Property(x => x.TestPlanId).HasConversion(new StronglyTypedIdValueConverter<TestPlanId>());
            e.Property(x => x.TestScenarioId).HasConversion(new StronglyTypedIdValueConverter<TestScenarioId>());
            e.Property(x => x.AssignedTesterId).HasMaxLength(100);
            e.HasOne(x => x.TestPlan).WithMany(p => p.Scenarios).HasForeignKey(x => x.TestPlanId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TestScenario).WithMany().HasForeignKey(x => x.TestScenarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.TestPlanId, x.TestScenarioId }).IsUnique().HasDatabaseName("ix_test_plan_scenarios_unique");
        });

        // ── TestExecution ────────────────────────────────────────
        modelBuilder.Entity<TestExecution>(e =>
        {
            e.ToTable("test_executions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<TestExecutionId>());
            e.Property(x => x.Result).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ExecutedBy).HasMaxLength(100).IsRequired();
            e.Property(x => x.Notes).HasColumnType("text");
            e.Property(x => x.Environment).HasMaxLength(500);
            e.Property(x => x.LinkedBugId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new WorkItemId(v.Value) : null).IsRequired(false);
            e.HasOne(x => x.TestPlanScenario).WithMany(ps => ps.Executions).HasForeignKey(x => x.TestPlanScenarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TestPlanScenarioId).HasDatabaseName("ix_test_executions_plan_scenario");
        });

        // ── TestStepResult ───────────────────────────────────────
        modelBuilder.Entity<TestStepResult>(e =>
        {
            e.ToTable("test_step_results");
            e.HasKey(x => x.Id);
            e.Property(x => x.TestExecutionId).HasConversion(new StronglyTypedIdValueConverter<TestExecutionId>());
            e.Property(x => x.TestStepId).HasConversion(new StronglyTypedIdValueConverter<TestStepId>());
            e.Property(x => x.Result).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ActualResult).HasMaxLength(2000);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasOne(x => x.TestExecution).WithMany(ex => ex.StepResults).HasForeignKey(x => x.TestExecutionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TestStep).WithMany().HasForeignKey(x => x.TestStepId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.TestExecutionId, x.TestStepId }).IsUnique().HasDatabaseName("ix_test_step_results_unique");
        });

        // ── Release ──────────────────────────────────────────────
        modelBuilder.Entity<Release>(e =>
        {
            e.ToTable("releases");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<ReleaseId>());
            e.Property(x => x.ProjectId).HasConversion(new StronglyTypedIdValueConverter<ProjectId>());
            e.Property(x => x.SprintId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new SprintId(v.Value) : null).IsRequired(false);
            e.Property(x => x.MilestoneId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new MilestoneId(v.Value) : null).IsRequired(false);
            e.Property(x => x.Key).HasMaxLength(30).IsRequired();
            e.Property(x => x.Version).HasMaxLength(50).IsRequired();
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Description).HasColumnType("text");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ReleaseManagerId).HasMaxLength(100);
            e.Property(x => x.TargetEnvironment).HasMaxLength(100);
            e.Property(x => x.Tags).HasMaxLength(500);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ProjectId, x.Key }).IsUnique().HasDatabaseName("ix_releases_project_key");
            e.HasIndex(x => x.Status).HasDatabaseName("ix_releases_status");
        });

        // ── ReleaseItem ──────────────────────────────────────────
        modelBuilder.Entity<ReleaseItem>(e =>
        {
            e.ToTable("release_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.ReleaseId).HasConversion(new StronglyTypedIdValueConverter<ReleaseId>());
            e.Property(x => x.WorkItemId).HasConversion(new StronglyTypedIdValueConverter<WorkItemId>());
            e.Property(x => x.IncludedBy).HasMaxLength(100).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(x => x.Release).WithMany(r => r.Items).HasForeignKey(x => x.ReleaseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.WorkItem).WithMany().HasForeignKey(x => x.WorkItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.ReleaseId, x.WorkItemId }).IsUnique().HasDatabaseName("ix_release_items_unique");
        });

        // ── GoNoGoChecklist ──────────────────────────────────────
        modelBuilder.Entity<GoNoGoChecklist>(e =>
        {
            e.ToTable("go_no_go_checklists");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<GoNoGoChecklistId>());
            e.Property(x => x.ReleaseId).HasConversion(new StronglyTypedIdValueConverter<ReleaseId>());
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.DecisionBy).HasMaxLength(100);
            e.Property(x => x.DecisionNotes).HasColumnType("text");
            e.HasOne(x => x.Release).WithOne(r => r.GoNoGoChecklist).HasForeignKey<GoNoGoChecklist>(x => x.ReleaseId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ReleaseId).IsUnique().HasDatabaseName("ix_go_no_go_checklists_release");
        });

        // ── GoNoGoItem ───────────────────────────────────────────
        modelBuilder.Entity<GoNoGoItem>(e =>
        {
            e.ToTable("go_no_go_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.ChecklistId).HasConversion(new StronglyTypedIdValueConverter<GoNoGoChecklistId>());
            e.Property(x => x.Category).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Description).HasColumnType("text");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ReviewedBy).HasMaxLength(100);
            e.Property(x => x.Notes).HasColumnType("text");
            e.HasOne(x => x.Checklist).WithMany(c => c.Items).HasForeignKey(x => x.ChecklistId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ChecklistId).HasDatabaseName("ix_go_no_go_items_checklist");
        });

        // ── ReleaseNote ──────────────────────────────────────────
        modelBuilder.Entity<ReleaseNote>(e =>
        {
            e.ToTable("release_notes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<ReleaseNoteId>());
            e.Property(x => x.ReleaseId).HasConversion(new StronglyTypedIdValueConverter<ReleaseId>());
            e.Property(x => x.Content).HasColumnType("text").IsRequired();
            e.HasOne(x => x.Release).WithOne(r => r.ReleaseNote).HasForeignKey<ReleaseNote>(x => x.ReleaseId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ReleaseId).IsUnique().HasDatabaseName("ix_release_notes_release");
        });
    }
}
