using EntApp.Modules.Finance.Application.DTOs;
using EntApp.Modules.Finance.Application.Queries;
using EntApp.Modules.Finance.Domain.Enums;
using EntApp.Modules.Finance.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Finance.Infrastructure.Handlers.Queries;

public sealed class ListAccountsQueryHandler(FinanceDbContext db)
    : IRequestHandler<ListAccountsQuery, PagedResult<AccountListDto>>
{
    public async Task<PagedResult<AccountListDto>> Handle(ListAccountsQuery request, CancellationToken ct)
    {
        var query = db.Accounts.Where(a => a.IsActive);
        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(a => a.Name.Contains(request.Search) || a.Code.Contains(request.Search));
        if (!string.IsNullOrEmpty(request.Type) && Enum.TryParse<AccountType>(request.Type, out var at))
            query = query.Where(a => a.AccountType == at);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(a => a.Code)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(a => new AccountListDto(a.Id.Value, a.Code, a.Name, a.AccountType.ToString(),
                a.Currency, a.Balance, a.TaxNumber, a.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<AccountListDto>
        { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}

public sealed class GetAccountQueryHandler(FinanceDbContext db)
    : IRequestHandler<GetAccountQuery, object?>
{
    public async Task<object?> Handle(GetAccountQuery request, CancellationToken ct)
        => await db.Accounts.FindAsync([request.Id], ct);
}

public sealed class GetBalanceSummaryQueryHandler(FinanceDbContext db)
    : IRequestHandler<GetBalanceSummaryQuery, List<BalanceSummaryDto>>
{
    public async Task<List<BalanceSummaryDto>> Handle(GetBalanceSummaryQuery request, CancellationToken ct)
    {
        return await db.Accounts.Where(a => a.IsActive)
            .GroupBy(a => a.AccountType)
            .Select(g => new BalanceSummaryDto(g.Key.ToString(), g.Count(), g.Sum(a => a.Balance)))
            .ToListAsync(ct);
    }
}

public sealed class ListInvoicesQueryHandler(FinanceDbContext db)
    : IRequestHandler<ListInvoicesQuery, PagedResult<InvoiceListDto>>
{
    public async Task<PagedResult<InvoiceListDto>> Handle(ListInvoicesQuery request, CancellationToken ct)
    {
        var query = db.Invoices.Include(i => i.Account).AsQueryable();
        if (!string.IsNullOrEmpty(request.Type) && Enum.TryParse<InvoiceType>(request.Type, out var it))
            query = query.Where(i => i.InvoiceType == it);
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<InvoiceStatus>(request.Status, out var s))
            query = query.Where(i => i.Status == s);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(i => i.InvoiceDate)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(i => new InvoiceListDto(i.Id.Value, i.InvoiceNumber, i.Account.Name,
                i.InvoiceType.ToString(), i.Status.ToString(),
                i.InvoiceDate, i.DueDate, i.GrandTotal, i.PaidAmount, i.Currency))
            .ToListAsync(ct);

        return new PagedResult<InvoiceListDto>
        { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}

public sealed class GetInvoiceQueryHandler(FinanceDbContext db)
    : IRequestHandler<GetInvoiceQuery, object?>
{
    public async Task<object?> Handle(GetInvoiceQuery request, CancellationToken ct)
    {
        return await db.Invoices.Include(i => i.Account).Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id.Value == request.Id, ct);
    }
}

public sealed class GetOverdueInvoicesQueryHandler(FinanceDbContext db)
    : IRequestHandler<GetOverdueInvoicesQuery, List<OverdueInvoiceDto>>
{
    public async Task<List<OverdueInvoiceDto>> Handle(GetOverdueInvoicesQuery request, CancellationToken ct)
    {
        return await db.Invoices.Include(i => i.Account)
            .Where(i => i.DueDate < DateTime.UtcNow
                && i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
            .OrderBy(i => i.DueDate)
            .Select(i => new OverdueInvoiceDto(i.Id.Value, i.InvoiceNumber, i.Account.Name,
                i.DueDate, i.GrandTotal, i.PaidAmount, (DateTime.UtcNow - i.DueDate).Days))
            .ToListAsync(ct);
    }
}

public sealed class ListPaymentsQueryHandler(FinanceDbContext db)
    : IRequestHandler<ListPaymentsQuery, PagedResult<PaymentListDto>>
{
    public async Task<PagedResult<PaymentListDto>> Handle(ListPaymentsQuery request, CancellationToken ct)
    {
        var query = db.Payments.Include(p => p.Account).AsQueryable();
        if (request.AccountId.HasValue) query = query.Where(p => p.AccountId.Value == request.AccountId.Value);
        if (!string.IsNullOrEmpty(request.Direction) && Enum.TryParse<PaymentDirection>(request.Direction, out var d))
            query = query.Where(p => p.Direction == d);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(p => p.PaymentDate)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(p => new PaymentListDto(p.Id.Value, p.Account.Name, p.Amount, p.Currency,
                p.Direction.ToString(), p.Method.ToString(), p.PaymentDate, p.ReferenceNumber))
            .ToListAsync(ct);

        return new PagedResult<PaymentListDto>
        { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}
