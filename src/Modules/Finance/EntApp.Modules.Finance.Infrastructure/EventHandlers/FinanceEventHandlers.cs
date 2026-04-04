using EntApp.Modules.Finance.Domain.Entities;
using EntApp.Modules.Finance.Domain.Enums;
using EntApp.Modules.Finance.Infrastructure.Persistence;
using EntApp.Modules.Sales.Application.IntegrationEvents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.Finance.Infrastructure.EventHandlers;

/// <summary>
/// Sipariş onaylandığında → otomatik satış faturası oluşturur.
/// Müşteri cari hesabını arar, yoksa otomatik oluşturur.
/// </summary>
public sealed class OrderConfirmedInvoiceHandler : INotificationHandler<OrderConfirmedEvent>
{
    private readonly FinanceDbContext _db;
    private readonly ILogger<OrderConfirmedInvoiceHandler> _logger;

    public OrderConfirmedInvoiceHandler(FinanceDbContext db, ILogger<OrderConfirmedInvoiceHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(OrderConfirmedEvent notification, CancellationToken cancellationToken)
    {
        // Müşteri cari hesabı bul veya oluştur
        var accountCode = $"CRM-{notification.CustomerId.ToString().Substring(0, 8)}";
        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Code == accountCode,
                cancellationToken);

        if (account is null)
        {
            account = AccountBase.Create(
                accountCode,
                notification.CustomerName ?? "Müşteri",
                AccountType.Customer);
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Fatura oluştur
        var invoiceNumber = $"INV-{notification.OrderNumber}";
        var existing = await _db.Invoices
            .AnyAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);
        if (existing)
        {
            _logger.LogWarning("OrderConfirmedInvoiceHandler: Fatura zaten mevcut. InvoiceNumber={InvoiceNumber}",
                invoiceNumber);
            return;
        }

        var invoice = InvoiceBase.Create(invoiceNumber, account.Id,
            InvoiceType.Sales, DateTime.UtcNow, DateTime.UtcNow.AddDays(30));

        foreach (var line in notification.Lines)
        {
            var invItem = InvoiceItemBase.Create(invoice.Id, line.ProductName,
                line.Quantity, line.UnitPrice, line.TaxRate);
            invoice.Items.Add(invItem);
        }

        invoice.Recalculate();
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("OrderConfirmedInvoiceHandler: Fatura oluşturuldu. InvoiceNumber={InvoiceNumber}, Total={Total}",
            invoiceNumber, invoice.GrandTotal);
    }
}
