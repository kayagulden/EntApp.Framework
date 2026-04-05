using EntApp.Modules.Finance.Domain.Entities;
using EntApp.Modules.Finance.Domain.Enums;
using EntApp.Modules.Finance.Domain.Ids;
using EntApp.Modules.Finance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Finance.Infrastructure.Endpoints;

/// <summary>Finance REST API endpoint'leri.</summary>
public static class FinanceEndpoints
{
    public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════ Accounts ═══════════
        var accs = app.MapGroup("/api/finance/accounts").WithTags("Finance - Accounts");

        accs.MapGet("/", async (FinanceDbContext db, string? search, string? type,
            int page = 1, int pageSize = 20) =>
        {
            var query = db.Accounts.Where(a => a.IsActive);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(a => a.Name.Contains(search) || a.Code.Contains(search));
            if (!string.IsNullOrEmpty(type) && Enum.TryParse<AccountType>(type, out var at))
                query = query.Where(a => a.AccountType == at);

            var total = await query.CountAsync();
            var items = await query.OrderBy(a => a.Code)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(a => new { a.Id, a.Code, a.Name, AccountType = a.AccountType.ToString(),
                    a.Currency, a.Balance, a.TaxNumber, a.CreatedAt })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListAccounts");

        accs.MapGet("/{id:guid}", async (Guid id, FinanceDbContext db) =>
        {
            var a = await db.Accounts.FindAsync(id);
            return a is null ? Results.NotFound() : Results.Ok(a);
        }).WithName("GetAccount");

        accs.MapPost("/", async (CreateAccountRequest req, FinanceDbContext db) =>
        {
            Enum.TryParse<AccountType>(req.AccountType, out var type);
            var account = AccountBase.Create(req.Code, req.Name, type, req.Currency ?? "TRY",
                req.TaxNumber, req.Email, req.Phone, req.Address);
            db.Accounts.Add(account);
            await db.SaveChangesAsync();
            return Results.Created($"/api/finance/accounts/{account.Id}", new { account.Id, account.Code });
        }).WithName("CreateAccount");

        // ── Balance Summary ──────────────────────────────
        accs.MapGet("/balance-summary", async (FinanceDbContext db) =>
        {
            var summary = await db.Accounts.Where(a => a.IsActive)
                .GroupBy(a => a.AccountType)
                .Select(g => new { AccountType = g.Key.ToString(),
                    Count = g.Count(), TotalBalance = g.Sum(a => a.Balance) })
                .ToListAsync();
            return Results.Ok(summary);
        }).WithName("AccountBalanceSummary").WithSummary("Hesap tipi bazlı bakiye özeti");

        // ═══════════ Invoices ═══════════
        var inv = app.MapGroup("/api/finance/invoices").WithTags("Finance - Invoices");

        inv.MapGet("/", async (FinanceDbContext db, string? type, string? status,
            int page = 1, int pageSize = 20) =>
        {
            var query = db.Invoices.Include(i => i.Account).AsQueryable();
            if (!string.IsNullOrEmpty(type) && Enum.TryParse<InvoiceType>(type, out var it))
                query = query.Where(i => i.InvoiceType == it);
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<InvoiceStatus>(status, out var s))
                query = query.Where(i => i.Status == s);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(i => i.InvoiceDate)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(i => new { i.Id, i.InvoiceNumber, AccountName = i.Account.Name,
                    InvoiceType = i.InvoiceType.ToString(), Status = i.Status.ToString(),
                    i.InvoiceDate, i.DueDate, i.GrandTotal, i.PaidAmount, i.Currency })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListInvoices");

        inv.MapGet("/{id:guid}", async (Guid id, FinanceDbContext db) =>
        {
            var invoice = await db.Invoices
                .Include(i => i.Account).Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id.Value == id);
            return invoice is null ? Results.NotFound() : Results.Ok(invoice);
        }).WithName("GetInvoice");

        inv.MapPost("/", async (CreateInvoiceRequest req, FinanceDbContext db) =>
        {
            Enum.TryParse<InvoiceType>(req.InvoiceType, out var type);
            var invoice = InvoiceBase.Create(req.InvoiceNumber, new AccountId(req.AccountId), type,
                req.InvoiceDate, req.DueDate, req.Currency ?? "TRY", req.Notes);

            // Kalemleri ekle
            foreach (var item in req.Items ?? [])
            {
                var invoiceItem = InvoiceItemBase.Create(invoice.Id, item.Description,
                    item.Quantity, item.UnitPrice, item.TaxRate, item.DiscountRate);
                invoice.Items.Add(invoiceItem);
            }

            invoice.Recalculate();
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();
            return Results.Created($"/api/finance/invoices/{invoice.Id}",
                new { invoice.Id, invoice.InvoiceNumber, invoice.GrandTotal });
        }).WithName("CreateInvoice");

        inv.MapPost("/{id:guid}/approve", async (Guid id, FinanceDbContext db) =>
        {
            var invoice = await db.Invoices.FindAsync(id);
            if (invoice is null) return Results.NotFound();
            invoice.Approve();
            await db.SaveChangesAsync();
            return Results.Ok(new { invoice.Id, Status = invoice.Status.ToString() });
        }).WithName("ApproveInvoice");

        // ── Overdue ──────────────────────────────────────
        inv.MapGet("/overdue", async (FinanceDbContext db) =>
        {
            var overdue = await db.Invoices.Include(i => i.Account)
                .Where(i => i.DueDate < DateTime.UtcNow
                    && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
                .OrderBy(i => i.DueDate)
                .Select(i => new { i.Id, i.InvoiceNumber, AccountName = i.Account.Name,
                    i.DueDate, i.GrandTotal, i.PaidAmount,
                    DaysOverdue = (DateTime.UtcNow - i.DueDate).Days })
                .ToListAsync();
            return Results.Ok(overdue);
        }).WithName("OverdueInvoices").WithSummary("Vadesi geçmiş faturalar");

        // ═══════════ Payments ═══════════
        var pay = app.MapGroup("/api/finance/payments").WithTags("Finance - Payments");

        pay.MapGet("/", async (FinanceDbContext db, Guid? accountId, string? direction,
            int page = 1, int pageSize = 20) =>
        {
            var query = db.Payments.Include(p => p.Account).AsQueryable();
            if (accountId.HasValue) query = query.Where(p => p.AccountId.Value == accountId.Value);
            if (!string.IsNullOrEmpty(direction) && Enum.TryParse<PaymentDirection>(direction, out var d))
                query = query.Where(p => p.Direction == d);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(p => p.PaymentDate)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new { p.Id, AccountName = p.Account.Name, p.Amount, p.Currency,
                    Direction = p.Direction.ToString(), Method = p.Method.ToString(),
                    p.PaymentDate, p.ReferenceNumber })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListPayments");

        pay.MapPost("/", async (CreatePaymentRequest req, FinanceDbContext db) =>
        {
            Enum.TryParse<PaymentDirection>(req.Direction, out var dir);
            Enum.TryParse<PaymentMethod>(req.Method, out var method);
            var payment = PaymentBase.Create(new AccountId(req.AccountId), req.Amount, dir, method,
                req.PaymentDate, req.InvoiceId.HasValue ? new InvoiceId(req.InvoiceId.Value) : null, req.Currency ?? "TRY",
                req.ReferenceNumber, req.Notes);

            db.Payments.Add(payment);

            // Cari bakiyeyi güncelle
            var account = await db.Accounts.FindAsync(req.AccountId);
            if (account is not null)
            {
                var balanceChange = dir == PaymentDirection.Incoming ? req.Amount : -req.Amount;
                account.UpdateBalance(balanceChange);
            }

            // Faturaya ödeme kaydet
            if (req.InvoiceId.HasValue)
            {
                var invoice = await db.Invoices.FindAsync(req.InvoiceId.Value);
                if (invoice is not null)
                {
                    invoice.RecordPayment(req.Amount);
                    invoice.UpdatePaymentStatus();
                }
            }

            await db.SaveChangesAsync();
            return Results.Created($"/api/finance/payments/{payment.Id}", new { payment.Id });
        }).WithName("CreatePayment");

        return app;
    }
}

// ── DTOs ─────────────────────────────────────────────────
public sealed record CreateAccountRequest(string Code, string Name, string AccountType = "Customer",
    string? Currency = null, string? TaxNumber = null, string? Email = null,
    string? Phone = null, string? Address = null);

public sealed record CreateInvoiceRequest(string InvoiceNumber, Guid AccountId,
    string InvoiceType = "Sales", DateTime InvoiceDate = default, DateTime DueDate = default,
    string? Currency = null, string? Notes = null, List<CreateInvoiceItemDto>? Items = null);

public sealed record CreateInvoiceItemDto(string Description, decimal Quantity = 1,
    decimal UnitPrice = 0, decimal TaxRate = 20, decimal DiscountRate = 0);

public sealed record CreatePaymentRequest(Guid AccountId, decimal Amount,
    string Direction = "Incoming", string Method = "BankTransfer",
    DateTime? PaymentDate = null, Guid? InvoiceId = null,
    string? Currency = null, string? ReferenceNumber = null, string? Notes = null);
