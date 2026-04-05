using EntApp.Modules.CRM.Domain.Entities;
using EntApp.Modules.CRM.Domain.Enums;
using EntApp.Modules.CRM.Domain.Ids;
using EntApp.Shared.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.CRM.Infrastructure.Persistence;

/// <summary>CRM modülü DbContext — schema: crm</summary>
public sealed class CrmDbContext : BaseDbContext
{
    public const string Schema = "crm";
    protected override string SchemaName => Schema;

    public DbSet<CustomerBase> Customers => Set<CustomerBase>();
    public DbSet<ContactBase> Contacts => Set<ContactBase>();
    public DbSet<OpportunityBase> Opportunities => Set<OpportunityBase>();
    public DbSet<ActivityBase> Activities => Set<ActivityBase>();

    public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Customer ──────────────────────────────────────────
        modelBuilder.Entity<CustomerBase>(e =>
        {
            e.ToTable("customers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<CustomerId>());
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.Code)
                .IsUnique()
                .HasFilter("\"Code\" IS NOT NULL AND \"IsDeleted\" = false");
            e.HasIndex(x => x.TaxNumber)
                .IsUnique()
                .HasFilter("\"TaxNumber\" IS NOT NULL AND \"IsDeleted\" = false");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.Country).HasMaxLength(100);
            e.Property(x => x.TaxNumber).HasMaxLength(20);
            e.Property(x => x.CustomerType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Segment).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        // ── Contact ───────────────────────────────────────────
        modelBuilder.Entity<ContactBase>(e =>
        {
            e.ToTable("contacts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<ContactId>());
            e.HasIndex(x => x.CustomerId);
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Title).HasMaxLength(100);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Department).HasMaxLength(100);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.Customer).WithMany(c => c.Contacts).HasForeignKey(x => x.CustomerId);
        });

        // ── Opportunity ───────────────────────────────────────
        modelBuilder.Entity<OpportunityBase>(e =>
        {
            e.ToTable("opportunities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<OpportunityId>());
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.Stage);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.LostReason).HasMaxLength(500);
            e.Property(x => x.EstimatedValue).HasPrecision(18, 2);
            e.Property(x => x.Stage).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.Customer).WithMany(c => c.Opportunities).HasForeignKey(x => x.CustomerId);
        });

        // ── Activity ──────────────────────────────────────────
        modelBuilder.Entity<ActivityBase>(e =>
        {
            e.ToTable("activities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<ActivityId>());
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.AssignedUserId);
            e.Property(x => x.Subject).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(5000);
            e.Property(x => x.ActivityType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.Customer).WithMany(c => c.Activities).HasForeignKey(x => x.CustomerId);
        });
    }
}
