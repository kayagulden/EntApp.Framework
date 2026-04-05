using EntApp.Modules.Sales.Domain.Entities;
using EntApp.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Sales.Infrastructure.Persistence;

/// <summary>Sales modülü DbContext — schema: sales</summary>
public sealed class SalesDbContext : BaseDbContext
{
    public const string Schema = "sales";
    protected override string SchemaName => Schema;

    public DbSet<SalesOrderBase> Orders => Set<SalesOrderBase>();
    public DbSet<OrderItemBase> OrderItems => Set<OrderItemBase>();
    public DbSet<PriceListBase> PriceLists => Set<PriceListBase>();

    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PriceListBase>(e =>
        {
            e.ToTable("price_lists");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code)
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.ListType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.PriceItemsJson).HasColumnType("jsonb");
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<SalesOrderBase>(e =>
        {
            e.ToTable("orders");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrderNumber)
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.OrderDate);
            e.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.CustomerName).HasMaxLength(200);
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.ShippingAddress).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.SubTotal).HasPrecision(18, 2);
            e.Property(x => x.TaxTotal).HasPrecision(18, 2);
            e.Property(x => x.DiscountTotal).HasPrecision(18, 2);
            e.Property(x => x.GrandTotal).HasPrecision(18, 2);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<OrderItemBase>(e =>
        {
            e.ToTable("order_items");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrderId);
            e.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
            e.Property(x => x.ProductSKU).HasMaxLength(50);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.TaxRate).HasPrecision(5, 2);
            e.Property(x => x.DiscountValue).HasPrecision(18, 4);
            e.Property(x => x.LineTotal).HasPrecision(18, 2);
            e.Property(x => x.TaxAmount).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.Property(x => x.DiscountType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId);
        });
    }
}
