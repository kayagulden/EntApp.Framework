using EntApp.Modules.RequestManagement.Domain.Entities;
using EntApp.Modules.RequestManagement.Domain.Ids;
using EntApp.Shared.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.RequestManagement.Infrastructure.Persistence;

/// <summary>RequestManagement modülü DbContext — schema: req</summary>
public sealed class RequestManagementDbContext : BaseDbContext
{
    public const string Schema = "req";
    protected override string SchemaName => Schema;

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<RequestCategory> Categories => Set<RequestCategory>();
    public DbSet<SlaDefinition> SlaDefinitions => Set<SlaDefinition>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketStatusHistory> TicketStatusHistory => Set<TicketStatusHistory>();
    public DbSet<ServiceQueue> ServiceQueues => Set<ServiceQueue>();
    public DbSet<QueueMembership> QueueMemberships => Set<QueueMembership>();

    public RequestManagementDbContext(DbContextOptions<RequestManagementDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Department ──────────────────────────────────────────
        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("departments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<DepartmentId>());
            e.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            e.HasIndex(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);

            e.Property(x => x.ParentDepartmentId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new DepartmentId(v.Value) : null);

            e.HasOne(x => x.ParentDepartment)
                .WithMany(x => x.SubDepartments)
                .HasForeignKey(x => x.ParentDepartmentId);
        });

        // ── SlaDefinition ───────────────────────────────────────
        modelBuilder.Entity<SlaDefinition>(e =>
        {
            e.ToTable("sla_definitions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<SlaDefinitionId>());
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.ResponseTimeJson).HasColumnType("jsonb");
            e.Property(x => x.ResolutionTimeJson).HasColumnType("jsonb");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── RequestCategory ─────────────────────────────────────
        modelBuilder.Entity<RequestCategory>(e =>
        {
            e.ToTable("request_categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<RequestCategoryId>());
            e.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.FormSchemaJson).HasColumnType("jsonb");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);

            e.Property(x => x.DepartmentId).HasConversion(new StronglyTypedIdValueConverter<DepartmentId>());
            e.Property(x => x.SlaDefinitionId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new SlaDefinitionId(v.Value) : null);

            e.HasOne(x => x.Department).WithMany(d => d.Categories).HasForeignKey(x => x.DepartmentId);
            e.HasOne(x => x.SlaDefinitionEntity).WithMany(s => s.Categories).HasForeignKey(x => x.SlaDefinitionId);
        });

        // ── Ticket ──────────────────────────────────────────────
        modelBuilder.Entity<Ticket>(e =>
        {
            e.ToTable("tickets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<TicketId>());
            e.HasIndex(x => x.Number).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Priority);
            e.HasIndex(x => x.AssigneeUserId);
            e.HasIndex(x => x.SlaResolutionDeadline);
            e.Property(x => x.Number).HasMaxLength(20).IsRequired();
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.Property(x => x.Description).HasMaxLength(5000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.FormDataJson).HasColumnType("jsonb");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);

            e.Property(x => x.CategoryId).HasConversion(new StronglyTypedIdValueConverter<RequestCategoryId>());
            e.Property(x => x.DepartmentId).HasConversion(new StronglyTypedIdValueConverter<DepartmentId>());

            e.HasOne(x => x.Category).WithMany(c => c.Tickets).HasForeignKey(x => x.CategoryId);
            e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
        });

        // ── TicketComment ───────────────────────────────────────
        modelBuilder.Entity<TicketComment>(e =>
        {
            e.ToTable("ticket_comments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<TicketCommentId>());
            e.HasIndex(x => x.TicketId);
            e.Property(x => x.Content).HasMaxLength(4000).IsRequired();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);

            e.Property(x => x.TicketId).HasConversion(new StronglyTypedIdValueConverter<TicketId>());
            e.HasOne(x => x.Ticket).WithMany(t => t.Comments).HasForeignKey(x => x.TicketId);
        });

        // ── TicketStatusHistory ─────────────────────────────────
        modelBuilder.Entity<TicketStatusHistory>(e =>
        {
            e.ToTable("ticket_status_history");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<TicketStatusHistoryId>());
            e.HasIndex(x => x.TicketId);
            e.Property(x => x.OldStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Reason).HasMaxLength(500);

            e.Property(x => x.TicketId).HasConversion(new StronglyTypedIdValueConverter<TicketId>());
            e.HasOne(x => x.Ticket).WithMany(t => t.StatusHistory).HasForeignKey(x => x.TicketId);
        });

        // ── ServiceQueue ────────────────────────────────────────
        modelBuilder.Entity<ServiceQueue>(e =>
        {
            e.ToTable("service_queues");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<ServiceQueueId>());
            e.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
            e.HasIndex(x => x.Name);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);

            e.Property(x => x.DepartmentId).HasConversion(
                v => v.HasValue ? v.Value.Value : (Guid?)null,
                v => v.HasValue ? new DepartmentId(v.Value) : null);

            e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
        });

        // ── QueueMembership ──────────────────────────────────────
        modelBuilder.Entity<QueueMembership>(e =>
        {
            e.ToTable("queue_memberships");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<QueueMembershipId>());
            e.HasIndex(x => new { x.QueueId, x.UserId }).IsUnique();
            e.Property(x => x.Role).HasMaxLength(50).IsRequired();

            e.Property(x => x.QueueId).HasConversion(new StronglyTypedIdValueConverter<ServiceQueueId>());
            e.HasOne(x => x.Queue).WithMany(q => q.Members).HasForeignKey(x => x.QueueId);
        });
    }
}
