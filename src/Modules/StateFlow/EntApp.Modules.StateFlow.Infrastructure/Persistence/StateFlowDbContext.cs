using EntApp.Modules.StateFlow.Domain.Entities;
using EntApp.Modules.StateFlow.Domain.Ids;
using EntApp.Shared.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.StateFlow.Infrastructure.Persistence;

/// <summary>StateFlow modülü DbContext — schema: sf</summary>
public sealed class StateFlowDbContext : BaseDbContext
{
    public const string Schema = "sf";
    protected override string SchemaName => Schema;

    public DbSet<StateFlowDefinition> FlowDefinitions => Set<StateFlowDefinition>();
    public DbSet<StateDefinition> StateDefinitions => Set<StateDefinition>();
    public DbSet<TransitionDefinition> TransitionDefinitions => Set<TransitionDefinition>();
    public DbSet<RuleExecutionLog> RuleExecutionLogs => Set<RuleExecutionLog>();

    public StateFlowDbContext(DbContextOptions<StateFlowDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── StateFlowDefinition ─────────────────────────────────
        modelBuilder.Entity<StateFlowDefinition>(e =>
        {
            e.ToTable("state_flow_definitions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<StateFlowDefinitionId>());
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Key).HasMaxLength(200).IsRequired();
            e.Property(x => x.Name).HasMaxLength(300).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);

            // EntityType + Key + Version benzersiz olmalı
            e.HasIndex(x => new { x.EntityType, x.Key, x.Version })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            // EntityType + Status — Published flow lookup (partial unique)
            // Her EntityType için en fazla 1 Published olabilir
            e.HasIndex(x => new { x.EntityType, x.Status })
                .IsUnique()
                .HasFilter("\"Status\" = 'Published' AND \"IsDeleted\" = false")
                .HasDatabaseName("ix_flow_definitions_entity_type_published");

            // TenantId index
            e.HasIndex(x => x.TenantId);

            // Global template lookup
            e.HasIndex(x => x.IsGlobalTemplate)
                .HasFilter("\"IsGlobalTemplate\" = true AND \"IsDeleted\" = false");
        });

        // ── StateDefinition ─────────────────────────────────────
        modelBuilder.Entity<StateDefinition>(e =>
        {
            e.ToTable("state_definitions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<StateDefinitionId>());
            e.Property(x => x.FlowDefinitionId).HasConversion(new StronglyTypedIdValueConverter<StateFlowDefinitionId>());
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Label).HasMaxLength(200).IsRequired();
            e.Property(x => x.Color).HasMaxLength(20);
            e.Property(x => x.Icon).HasMaxLength(50);
            e.Property(x => x.Category).HasMaxLength(50);
            e.Property(x => x.OnEntryActions).HasColumnType("jsonb");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);

            // Aynı flow içinde aynı isimde state olamaz
            e.HasIndex(x => new { x.FlowDefinitionId, x.Name })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            e.HasOne(x => x.FlowDefinition)
                .WithMany(f => f.States)
                .HasForeignKey(x => x.FlowDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── TransitionDefinition ────────────────────────────────
        modelBuilder.Entity<TransitionDefinition>(e =>
        {
            e.ToTable("transition_definitions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<TransitionDefinitionId>());
            e.Property(x => x.FlowDefinitionId).HasConversion(new StronglyTypedIdValueConverter<StateFlowDefinitionId>());
            e.Property(x => x.FromStateName).HasMaxLength(100).IsRequired();
            e.Property(x => x.ToStateName).HasMaxLength(100).IsRequired();
            e.Property(x => x.TriggerName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Label).HasMaxLength(200).IsRequired();
            e.Property(x => x.RequiredRole).HasMaxLength(100);
            e.Property(x => x.GuardExpression).HasMaxLength(500);
            e.Property(x => x.OnTransitionActions).HasColumnType("jsonb");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);

            // Index: flow + from + trigger benzersiz
            e.HasIndex(x => new { x.FlowDefinitionId, x.FromStateName, x.TriggerName })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            e.HasOne(x => x.FlowDefinition)
                .WithMany(f => f.Transitions)
                .HasForeignKey(x => x.FlowDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── RuleExecutionLog ────────────────────────────────────
        modelBuilder.Entity<RuleExecutionLog>(e =>
        {
            e.ToTable("rule_execution_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<RuleExecutionLogId>());
            e.Property(x => x.FlowDefinitionId).HasConversion(new StronglyTypedIdValueConverter<StateFlowDefinitionId>());
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.Source).HasMaxLength(30).IsRequired();
            e.Property(x => x.StateName).HasMaxLength(100).IsRequired();
            e.Property(x => x.TriggerName).HasMaxLength(100);
            e.Property(x => x.ActionType).HasMaxLength(50).IsRequired();
            e.Property(x => x.ActionParamsJson).HasColumnType("jsonb");
            e.Property(x => x.ErrorMessage).HasColumnType("text");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);

            e.HasIndex(x => x.FlowDefinitionId).HasDatabaseName("ix_rule_execution_logs_flow");
            e.HasIndex(x => x.TargetEntityId).HasDatabaseName("ix_rule_execution_logs_entity");
            e.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_rule_execution_logs_created");
            e.HasIndex(x => x.TenantId);
        });
    }
}
