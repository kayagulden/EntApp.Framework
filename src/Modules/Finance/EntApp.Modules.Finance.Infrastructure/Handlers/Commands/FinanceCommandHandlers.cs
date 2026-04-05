using EntApp.Modules.Finance.Application.Commands;
using EntApp.Modules.Finance.Domain.Entities;
using EntApp.Modules.Finance.Domain.Enums;
using EntApp.Modules.Finance.Domain.Ids;
using EntApp.Modules.Finance.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Finance.Infrastructure.Handlers.Commands;

public sealed class CreateAccountCommandHandler(FinanceDbContext db)
    : IRequestHandler<CreateAccountCommand, Guid>
{
    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken ct)
    {
        Enum.TryParse<AccountType>(request.AccountType, out var type);
        var account = AccountBase.Create(request.Code, request.Name, type,
            request.Currency ?? "TRY", request.TaxNumber, request.Email, request.Phone, request.Address);
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);
        return account.Id.Value;
    }
}

public sealed class CreateInvoiceCommandHandler(FinanceDbContext db)
    : IRequestHandler<CreateInvoiceCommand, CreateInvoiceResult>
{
    public async Task<CreateInvoiceResult> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
        Enum.TryParse<InvoiceType>(request.InvoiceType, out var type);
        var invoice = InvoiceBase.Create(request.InvoiceNumber, new AccountId(request.AccountId),
            type, request.InvoiceDate, request.DueDate, request.Currency ?? "TRY", request.Notes);

        foreach (var item in request.Items ?? [])
        {
            var invoiceItem = InvoiceItemBase.Create(invoice.Id, item.Description,
                item.Quantity, item.UnitPrice, item.TaxRate, item.DiscountRate);
            invoice.Items.Add(invoiceItem);
        }

        invoice.Recalculate();
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        return new CreateInvoiceResult(invoice.Id.Value, invoice.InvoiceNumber, invoice.GrandTotal);
    }
}

public sealed class ApproveInvoiceCommandHandler(FinanceDbContext db)
    : IRequestHandler<ApproveInvoiceCommand, string>
{
    public async Task<string> Handle(ApproveInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await db.Invoices.FindAsync([request.InvoiceId], ct)
            ?? throw new KeyNotFoundException($"Invoice {request.InvoiceId} not found");
        invoice.Approve();
        await db.SaveChangesAsync(ct);
        return invoice.Status.ToString();
    }
}

public sealed class CreatePaymentCommandHandler(FinanceDbContext db)
    : IRequestHandler<CreatePaymentCommand, Guid>
{
    public async Task<Guid> Handle(CreatePaymentCommand request, CancellationToken ct)
    {
        Enum.TryParse<PaymentDirection>(request.Direction, out var dir);
        Enum.TryParse<PaymentMethod>(request.Method, out var method);
        var payment = PaymentBase.Create(new AccountId(request.AccountId), request.Amount, dir, method,
            request.PaymentDate, request.InvoiceId.HasValue ? new InvoiceId(request.InvoiceId.Value) : null,
            request.Currency ?? "TRY", request.ReferenceNumber, request.Notes);

        db.Payments.Add(payment);

        var account = await db.Accounts.FindAsync([request.AccountId], ct);
        if (account is not null)
        {
            var balanceChange = dir == PaymentDirection.Incoming ? request.Amount : -request.Amount;
            account.UpdateBalance(balanceChange);
        }

        if (request.InvoiceId.HasValue)
        {
            var invoice = await db.Invoices.FindAsync([request.InvoiceId.Value], ct);
            if (invoice is not null)
            {
                invoice.RecordPayment(request.Amount);
                invoice.UpdatePaymentStatus();
            }
        }

        await db.SaveChangesAsync(ct);
        return payment.Id.Value;
    }
}
