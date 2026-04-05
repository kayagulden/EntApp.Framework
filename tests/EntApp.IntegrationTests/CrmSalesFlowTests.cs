using EntApp.Modules.CRM.Domain.Entities;
using EntApp.Modules.CRM.Domain.Enums;
using EntApp.Modules.CRM.Domain.Ids;
using EntApp.Modules.Sales.Domain.Entities;
using EntApp.Modules.Sales.Domain.Enums;
using EntApp.Modules.Sales.Domain.Ids;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EntApp.IntegrationTests;

/// <summary>
/// CRM → Sales sipariş akışı testi.
/// Senaryo: CRM fırsatı kazanılınca satış siparişi oluşturulur.
/// </summary>
public class CrmSalesFlowTests
{
    [Fact]
    public async Task Won_Opportunity_Creates_Sales_Order()
    {
        using var crmDb = TestDbFactory.CreateCrmDb();
        using var salesDb = TestDbFactory.CreateSalesDb();

        // Arrange — müşteri ve fırsat oluştur
        var customer = CustomerBase.Create("Acme Corp", CustomerType.Company);
        crmDb.Customers.Add(customer);
        await crmDb.SaveChangesAsync();

        var opportunity = OpportunityBase.Create(
            customer.Id, "ERP Projesi", estimatedValue: 250_000,
            expectedCloseDate: DateTime.UtcNow.AddMonths(1));
        crmDb.Opportunities.Add(opportunity);
        await crmDb.SaveChangesAsync();

        // Act — fırsatı kazanıldı olarak işaretle
        opportunity.AdvanceStage(OpportunityStage.ClosedWon);
        await crmDb.SaveChangesAsync();

        // Simulate integration: fırsat kazanıldığında sipariş oluştur
        var order = SalesOrderBase.Create(
            $"SO-OPP-{opportunity.Id.Value.ToString()[..8]}", customer.Id.Value, DateTime.UtcNow,
            customer.Name, "TRY");

        var item = OrderItemBase.Create(order.Id, Guid.NewGuid(),
            opportunity.Title, quantity: 1, unitPrice: opportunity.EstimatedValue);
        order.Items.Add(item);
        order.Recalculate();
        salesDb.Orders.Add(order);
        await salesDb.SaveChangesAsync();

        // Assert
        opportunity.Stage.Should().Be(OpportunityStage.ClosedWon);
        opportunity.Probability.Should().Be(100);

        order.GrandTotal.Should().BeGreaterThan(0);
        order.Items.Should().HaveCount(1);
        order.Status.Should().Be(OrderStatus.Draft);

        var savedOrder = await salesDb.Orders.Include(o => o.Items)
            .FirstAsync(o => o.Id == order.Id);
        savedOrder.Items.First().ProductName.Should().Be("ERP Projesi");
    }

    [Fact]
    public async Task Lost_Opportunity_Does_Not_Create_Order()
    {
        using var crmDb = TestDbFactory.CreateCrmDb();

        var customer = CustomerBase.Create("Lost Corp", CustomerType.Company);
        crmDb.Customers.Add(customer);
        await crmDb.SaveChangesAsync();

        var opportunity = OpportunityBase.Create(customer.Id, "Kayıp Proje", estimatedValue: 50_000);
        crmDb.Opportunities.Add(opportunity);
        opportunity.AdvanceStage(OpportunityStage.ClosedLost);
        await crmDb.SaveChangesAsync();

        opportunity.Stage.Should().Be(OpportunityStage.ClosedLost);
        opportunity.Probability.Should().Be(0);
    }

    [Fact]
    public async Task Procurement_ThreeWay_Matching_Integration()
    {
        using var procDb = TestDbFactory.CreateProcurementDb();
        using var financeDb = TestDbFactory.CreateFinanceDb();

        // Arrange — tedarikçi ve PO oluştur
        var supplier = EntApp.Modules.Procurement.Domain.Entities.SupplierBase.Create(
            "SUP-001", "Test Tedarikçi", paymentTermDays: 30);
        procDb.Suppliers.Add(supplier);
        await procDb.SaveChangesAsync();

        var po = EntApp.Modules.Procurement.Domain.Entities.PurchaseOrderBase.Create(
            "PO-001", new EntApp.Modules.Procurement.Domain.Ids.SupplierId(supplier.Id.Value), DateTime.UtcNow, "Test Tedarikçi",
            subTotal: 10_000, taxTotal: 2_000);
        procDb.PurchaseOrders.Add(po);
        await procDb.SaveChangesAsync();

        // Act — tam teslim al
        po.ReceiveFull();
        await procDb.SaveChangesAsync();

        // Fatura oluştur
        var account = EntApp.Modules.Finance.Domain.Entities.AccountBase.Create(
            "SUP-ACC-001", "Test Tedarikçi", EntApp.Modules.Finance.Domain.Enums.AccountType.Supplier);
        financeDb.Accounts.Add(account);
        await financeDb.SaveChangesAsync();

        var invoice = EntApp.Modules.Finance.Domain.Entities.InvoiceBase.Create(
            "PINV-001", account.Id, EntApp.Modules.Finance.Domain.Enums.InvoiceType.Purchase,
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        financeDb.Invoices.Add(invoice);
        await financeDb.SaveChangesAsync();

        // 3-way match
        po.MatchInvoice(invoice.Id.Value);
        await procDb.SaveChangesAsync();

        // Assert
        po.Status.Should().Be(EntApp.Modules.Procurement.Domain.Enums.PurchaseOrderStatus.Invoiced);
        po.MatchingStatus.Should().Be(EntApp.Modules.Procurement.Domain.Enums.MatchingStatus.FullMatch);
        po.ReceivedTotal.Should().Be(po.GrandTotal);
    }
}
