using EntApp.Modules.Procurement.Domain.Entities;
using EntApp.Modules.Procurement.Domain.Ids;
using EntApp.Shared.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Procurement.Infrastructure.Persistence;

/// <summary>Procurement modülü DbContext — schema: proc</summary>
public sealed class ProcurementDbContext : BaseDbContext
{
    public const string Schema = "proc";
    protected override string SchemaName => Schema;

    public DbSet<SupplierBase> Suppliers => Set<SupplierBase>();
    public DbSet<PurchaseRequestBase> PurchaseRequests => Set<PurchaseRequestBase>();
    public DbSet<PurchaseOrderBase> PurchaseOrders => Set<PurchaseOrderBase>();

    public ProcurementDbContext(DbContextOptions<ProcurementDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SupplierBase>(e =>
        {
            e.ToTable("suppliers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<SupplierId>());
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Name);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.TaxNumber).HasMaxLength(20);
            e.Property(x => x.ContactPerson).HasMaxLength(100);
            e.Property(x => x.Rating).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<PurchaseRequestBase>(e =>
        {
            e.ToTable("purchase_requests");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<PurchaseRequestId>());
            e.HasIndex(x => x.RequestNumber).IsUnique();
            e.HasIndex(x => x.Status);
            e.Property(x => x.RequestNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.Department).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ItemsJson).HasColumnType("jsonb");
            e.Property(x => x.EstimatedTotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<PurchaseOrderBase>(e =>
        {
            e.ToTable("purchase_orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<PurchaseOrderId>());
            e.Property(x => x.SupplierId).HasConversion(new StronglyTypedIdValueConverter<SupplierId>());
            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.HasIndex(x => x.SupplierId);
            e.HasIndex(x => x.Status);
            e.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.SupplierName).HasMaxLength(200);
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.MatchingStatus).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ItemsJson).HasColumnType("jsonb");
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.TaxTotal).HasPrecision(18, 2);
            e.Property(x => x.GrandTotal).HasPrecision(18, 2);
            e.Property(x => x.ReceivedTotal).HasPrecision(18, 2);
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId);
        });
    }
}
