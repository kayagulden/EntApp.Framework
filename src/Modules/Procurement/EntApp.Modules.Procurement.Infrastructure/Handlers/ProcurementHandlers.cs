using EntApp.Modules.Procurement.Application.Commands;
using EntApp.Modules.Procurement.Application.Queries;
using EntApp.Modules.Procurement.Domain.Entities;
using EntApp.Modules.Procurement.Domain.Enums;
using EntApp.Modules.Procurement.Domain.Ids;
using EntApp.Modules.Procurement.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Procurement.Infrastructure.Handlers;

// ── Queries ─────────────────────────────────────────────────
public sealed class ListSuppliersQueryHandler(ProcurementDbContext db) : IRequestHandler<ListSuppliersQuery, PagedResult<object>>
{
    public async Task<PagedResult<object>> Handle(ListSuppliersQuery request, CancellationToken ct)
    {
        var query = db.Suppliers.Where(s => s.IsActive);
        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(s => s.Name.Contains(request.Search) || s.Code.Contains(request.Search));
        if (!string.IsNullOrEmpty(request.Rating) && Enum.TryParse<SupplierRating>(request.Rating, out var r))
            query = query.Where(s => s.Rating == r);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(s => s.Name).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(s => (object)new { s.Id, s.Code, s.Name, s.Email, s.Phone, s.ContactPerson, Rating = s.Rating.ToString(), s.PaymentTermDays })
            .ToListAsync(ct);
        return new PagedResult<object> { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}

public sealed class ListPurchaseRequestsQueryHandler(ProcurementDbContext db) : IRequestHandler<ListPurchaseRequestsQuery, PagedResult<object>>
{
    public async Task<PagedResult<object>> Handle(ListPurchaseRequestsQuery request, CancellationToken ct)
    {
        var query = db.PurchaseRequests.AsQueryable();
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<PurchaseRequestStatus>(request.Status, out var s))
            query = query.Where(r => r.Status == s);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(r => r.CreatedAt).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(r => (object)new { r.Id, r.RequestNumber, r.Department, Status = r.Status.ToString(), r.EstimatedTotal, r.Currency, r.RequiredByDate, r.CreatedAt })
            .ToListAsync(ct);
        return new PagedResult<object> { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}

public sealed class ListPurchaseOrdersQueryHandler(ProcurementDbContext db) : IRequestHandler<ListPurchaseOrdersQuery, PagedResult<object>>
{
    public async Task<PagedResult<object>> Handle(ListPurchaseOrdersQuery request, CancellationToken ct)
    {
        var query = db.PurchaseOrders.Include(o => o.Supplier).AsQueryable();
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<PurchaseOrderStatus>(request.Status, out var s))
            query = query.Where(o => o.Status == s);
        if (request.SupplierId.HasValue) query = query.Where(o => o.SupplierId.Value == request.SupplierId.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(o => o.OrderDate).Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(o => (object)new { o.Id, o.OrderNumber, SupplierName = o.Supplier.Name, Status = o.Status.ToString(),
                o.OrderDate, o.GrandTotal, o.ReceivedTotal, MatchingStatus = o.MatchingStatus.ToString() })
            .ToListAsync(ct);
        return new PagedResult<object> { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}

public sealed class GetPurchaseOrderQueryHandler(ProcurementDbContext db) : IRequestHandler<GetPurchaseOrderQuery, object?>
{
    public async Task<object?> Handle(GetPurchaseOrderQuery request, CancellationToken ct)
        => await db.PurchaseOrders.Include(x => x.Supplier).FirstOrDefaultAsync(x => x.Id.Value == request.Id, ct);
}

// ── Commands ────────────────────────────────────────────────
public sealed class CreateSupplierCommandHandler(ProcurementDbContext db) : IRequestHandler<CreateSupplierCommand, Guid>
{
    public async Task<Guid> Handle(CreateSupplierCommand request, CancellationToken ct)
    {
        var supplier = SupplierBase.Create(request.Code, request.Name, request.Email, request.Phone,
            request.Address, request.TaxNumber, request.ContactPerson, request.PaymentTermDays);
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(ct);
        return supplier.Id.Value;
    }
}

public sealed class RateSupplierCommandHandler(ProcurementDbContext db) : IRequestHandler<RateSupplierCommand, string>
{
    public async Task<string> Handle(RateSupplierCommand request, CancellationToken ct)
    {
        var supplier = await db.Suppliers.FindAsync([request.SupplierId], ct)
            ?? throw new KeyNotFoundException($"Supplier {request.SupplierId} not found");
        if (!Enum.TryParse<SupplierRating>(request.Rating, out var rating))
            throw new ArgumentException($"Invalid rating: {request.Rating}");
        supplier.Rate(rating);
        await db.SaveChangesAsync(ct);
        return supplier.Rating.ToString();
    }
}

public sealed class CreatePurchaseRequestCommandHandler(ProcurementDbContext db) : IRequestHandler<CreatePurchaseRequestCommand, Guid>
{
    public async Task<Guid> Handle(CreatePurchaseRequestCommand request, CancellationToken ct)
    {
        var pr = PurchaseRequestBase.Create(request.RequestNumber, request.RequestedByUserId,
            request.Department, request.Description, request.ItemsJson,
            request.EstimatedTotal, request.Currency ?? "TRY", request.RequiredByDate);
        pr.Submit();
        db.PurchaseRequests.Add(pr);
        await db.SaveChangesAsync(ct);
        return pr.Id.Value;
    }
}

public sealed class ApprovePurchaseRequestCommandHandler(ProcurementDbContext db) : IRequestHandler<ApprovePurchaseRequestCommand, string>
{
    public async Task<string> Handle(ApprovePurchaseRequestCommand request, CancellationToken ct)
    {
        var pr = await db.PurchaseRequests.FindAsync([request.RequestId], ct)
            ?? throw new KeyNotFoundException($"Request {request.RequestId} not found");
        pr.Approve();
        await db.SaveChangesAsync(ct);
        return pr.Status.ToString();
    }
}

public sealed class RejectPurchaseRequestCommandHandler(ProcurementDbContext db) : IRequestHandler<RejectPurchaseRequestCommand, string>
{
    public async Task<string> Handle(RejectPurchaseRequestCommand request, CancellationToken ct)
    {
        var pr = await db.PurchaseRequests.FindAsync([request.RequestId], ct)
            ?? throw new KeyNotFoundException($"Request {request.RequestId} not found");
        pr.Reject();
        await db.SaveChangesAsync(ct);
        return pr.Status.ToString();
    }
}

public sealed class CreatePurchaseOrderCommandHandler(ProcurementDbContext db) : IRequestHandler<CreatePurchaseOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreatePurchaseOrderCommand request, CancellationToken ct)
    {
        var po = PurchaseOrderBase.Create(request.OrderNumber, new SupplierId(request.SupplierId),
            request.OrderDate == default ? DateTime.UtcNow : request.OrderDate,
            request.SupplierName, request.Currency ?? "TRY", request.ExpectedDeliveryDate,
            request.ItemsJson, request.SubTotal, request.TaxTotal, request.Notes);
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync(ct);
        return po.Id.Value;
    }
}

public sealed class ReceivePurchaseOrderCommandHandler(ProcurementDbContext db) : IRequestHandler<ReceivePurchaseOrderCommand, ReceiveResult>
{
    public async Task<ReceiveResult> Handle(ReceivePurchaseOrderCommand request, CancellationToken ct)
    {
        var order = await db.PurchaseOrders.FindAsync([request.OrderId], ct)
            ?? throw new KeyNotFoundException($"Order {request.OrderId} not found");
        if (request.Full) order.ReceiveFull();
        else order.ReceivePartial(request.Amount);
        await db.SaveChangesAsync(ct);
        return new ReceiveResult(order.Id.Value, order.Status.ToString(), order.ReceivedTotal);
    }
}

public sealed class MatchInvoiceCommandHandler(ProcurementDbContext db) : IRequestHandler<MatchInvoiceCommand, string>
{
    public async Task<string> Handle(MatchInvoiceCommand request, CancellationToken ct)
    {
        var order = await db.PurchaseOrders.FindAsync([request.OrderId], ct)
            ?? throw new KeyNotFoundException($"Order {request.OrderId} not found");
        order.MatchInvoice(request.InvoiceId);
        await db.SaveChangesAsync(ct);
        return order.MatchingStatus.ToString();
    }
}
