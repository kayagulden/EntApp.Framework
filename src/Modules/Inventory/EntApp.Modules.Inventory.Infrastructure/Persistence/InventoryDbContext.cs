using EntApp.Modules.Inventory.Domain.Entities;
using EntApp.Modules.Inventory.Domain.Ids;
using EntApp.Shared.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Inventory.Infrastructure.Persistence;

/// <summary>Inventory modülü DbContext — schema: inv</summary>
public sealed class InventoryDbContext : BaseDbContext
{
    public const string Schema = "inv";
    protected override string SchemaName => Schema;

    public DbSet<ProductBase> Products => Set<ProductBase>();
    public DbSet<WarehouseBase> Warehouses => Set<WarehouseBase>();
    public DbSet<StockMovementBase> StockMovements => Set<StockMovementBase>();

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductBase>(e =>
        {
            e.ToTable("products");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<ProductId>());
            e.HasIndex(x => x.SKU).IsUnique();
            e.HasIndex(x => x.Barcode).HasFilter("\"Barcode\" IS NOT NULL");
            e.HasIndex(x => x.Category);
            e.Property(x => x.SKU).HasMaxLength(50).IsRequired();
            e.Property(x => x.Barcode).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.ProductType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Unit).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.UnitPrice).HasPrecision(18, 4);
            e.Property(x => x.CostPrice).HasPrecision(18, 4);
            e.Property(x => x.MinStock).HasPrecision(18, 4);
            e.Property(x => x.MaxStock).HasPrecision(18, 4);
            e.Property(x => x.ReorderPoint).HasPrecision(18, 4);
        });

        modelBuilder.Entity<WarehouseBase>(e =>
        {
            e.ToTable("warehouses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<WarehouseId>());
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<StockMovementBase>(e =>
        {
            e.ToTable("stock_movements");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(new StronglyTypedIdValueConverter<StockMovementId>());
            e.Property(x => x.ProductId).HasConversion(new StronglyTypedIdValueConverter<ProductId>());
            e.Property(x => x.WarehouseId).HasConversion(new StronglyTypedIdValueConverter<WarehouseId>());
            e.Property(x => x.TargetWarehouseId).HasConversion(new StronglyTypedIdValueConverter<WarehouseId>());
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => x.WarehouseId);
            e.HasIndex(x => x.MovementDate);
            e.Property(x => x.Quantity).HasPrecision(18, 4);
            e.Property(x => x.UnitCost).HasPrecision(18, 4);
            e.Property(x => x.MovementType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ReferenceNumber).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId);
            e.HasOne(x => x.TargetWarehouse).WithMany().HasForeignKey(x => x.TargetWarehouseId);
        });
    }
}
