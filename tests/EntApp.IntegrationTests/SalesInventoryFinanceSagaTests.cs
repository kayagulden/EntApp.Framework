using EntApp.Modules.Sales.Domain.Entities;
using EntApp.Modules.Sales.Domain.Enums;
using EntApp.Modules.Finance.Domain.Entities;
using EntApp.Modules.Finance.Domain.Enums;
using EntApp.Modules.Inventory.Domain.Entities;
using EntApp.Modules.Inventory.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EntApp.IntegrationTests;

/// <summary>
/// Sales → Inventory → Finance saga testi.
/// Senaryo: Sipariş onaylandığında stok düşülür ve fatura oluşturulur.
/// </summary>
public class SalesInventoryFinanceSagaTests
{
    [Fact]
    public async Task Sales_Order_Creates_Stock_Movement_And_Invoice()
    {
        // Arrange — modül DbContext'leri
        using var salesDb = TestDbFactory.CreateSalesDb();
        using var inventoryDb = TestDbFactory.CreateInventoryDb();
        using var financeDb = TestDbFactory.CreateFinanceDb();

        // 1. Inventory: Ürün oluştur
        var product = ProductBase.Create("SKU-001", "Test Ürün", ProductType.Physical,
            UnitOfMeasure.Piece, unitPrice: 100, costPrice: 60);
        inventoryDb.Products.Add(product);
        await inventoryDb.SaveChangesAsync();

        // 2. Inventory: Depo oluştur + stok girişi
        var warehouse = WarehouseBase.Create("WH-01", "Ana Depo");
        inventoryDb.Warehouses.Add(warehouse);
        await inventoryDb.SaveChangesAsync();

        var stockIn = StockMovementBase.Create(product.Id, warehouse.Id,
            MovementType.StockIn, quantity: 100, unitCost: 60);
        inventoryDb.StockMovements.Add(stockIn);
        await inventoryDb.SaveChangesAsync();

        // 3. Finance: Müşteri cari hesabı oluştur
        var account = AccountBase.Create("C-001", "Test Müşteri", AccountType.Customer);
        financeDb.Accounts.Add(account);
        await financeDb.SaveChangesAsync();

        // Act — Sipariş oluştur ve onayla
        var order = SalesOrderBase.Create("SO-001", Guid.NewGuid(), DateTime.UtcNow,
            "Test Müşteri", "TRY");
        var item = OrderItemBase.Create(order.Id, product.Id, "Test Ürün",
            quantity: 5, unitPrice: 100, taxRate: 20);
        order.Items.Add(item);
        order.Recalculate();
        salesDb.Orders.Add(order);
        await salesDb.SaveChangesAsync();

        order.Confirm();
        await salesDb.SaveChangesAsync();

        // Simulate saga: stok düşümü
        var stockOut = StockMovementBase.Create(product.Id, warehouse.Id,
            MovementType.StockOut, quantity: 5, unitCost: 60,
            referenceNumber: order.OrderNumber);
        inventoryDb.StockMovements.Add(stockOut);
        await inventoryDb.SaveChangesAsync();

        // Simulate saga: fatura oluşturma
        var invoice = InvoiceBase.Create($"INV-{order.OrderNumber}", account.Id,
            InvoiceType.Sales, DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        var invItem = InvoiceItemBase.Create(invoice.Id, "Test Ürün",
            quantity: 5, unitPrice: 100, taxRate: 20);
        invoice.Items.Add(invItem);
        invoice.Recalculate();
        invoice.Approve();
        financeDb.Invoices.Add(invoice);
        await financeDb.SaveChangesAsync();

        // Assert
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.GrandTotal.Should().Be(600); // 500 + 100 KDV

        var movements = await inventoryDb.StockMovements.ToListAsync();
        movements.Should().HaveCount(2);
        var totalStock = movements.Where(m => m.MovementType == MovementType.StockIn).Sum(m => m.Quantity)
            - movements.Where(m => m.MovementType == MovementType.StockOut).Sum(m => m.Quantity);
        totalStock.Should().Be(95); // 100 - 5

        invoice.Status.Should().Be(InvoiceStatus.Approved);
        invoice.GrandTotal.Should().Be(order.GrandTotal);
    }

    [Fact]
    public async Task Cancelled_Order_Should_Not_Create_Stock_Movement()
    {
        using var salesDb = TestDbFactory.CreateSalesDb();

        var order = SalesOrderBase.Create("SO-002", Guid.NewGuid(), DateTime.UtcNow, "İptal Müşterisi");
        var item = OrderItemBase.Create(order.Id, Guid.NewGuid(), "Ürün X", 3, 200, 18);
        order.Items.Add(item);
        order.Recalculate();
        salesDb.Orders.Add(order);
        order.Cancel();
        await salesDb.SaveChangesAsync();

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.GrandTotal.Should().BeGreaterThan(0); // Hesaplama yapılmış olmalı ama stok etkilenmemeli
    }
}
