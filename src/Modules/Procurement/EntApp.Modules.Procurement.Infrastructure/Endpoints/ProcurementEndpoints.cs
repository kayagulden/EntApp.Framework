using EntApp.Modules.Procurement.Domain.Entities;
using EntApp.Modules.Procurement.Domain.Enums;
using EntApp.Modules.Procurement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Procurement.Infrastructure.Endpoints;

/// <summary>Procurement REST API endpoint'leri.</summary>
public static class ProcurementEndpoints
{
    public static IEndpointRouteBuilder MapProcurementEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════ Suppliers ═══════════
        var sup = app.MapGroup("/api/procurement/suppliers").WithTags("Procurement - Suppliers");

        sup.MapGet("/", async (ProcurementDbContext db, string? search, string? rating,
            int page = 1, int pageSize = 20) =>
        {
            var query = db.Suppliers.Where(s => s.IsActive);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.Name.Contains(search) || s.Code.Contains(search));
            if (!string.IsNullOrEmpty(rating) && Enum.TryParse<SupplierRating>(rating, out var r))
                query = query.Where(s => s.Rating == r);

            var total = await query.CountAsync();
            var items = await query.OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(s => new { s.Id, s.Code, s.Name, s.Email, s.Phone,
                    s.ContactPerson, Rating = s.Rating.ToString(), s.PaymentTermDays })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListSuppliers");

        sup.MapPost("/", async (CreateSupplierRequest req, ProcurementDbContext db) =>
        {
            var supplier = SupplierBase.Create(req.Code, req.Name, req.Email, req.Phone,
                req.Address, req.TaxNumber, req.ContactPerson, req.PaymentTermDays);
            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync();
            return Results.Created($"/api/procurement/suppliers/{supplier.Id}",
                new { supplier.Id, supplier.Code });
        }).WithName("CreateSupplier");

        sup.MapPost("/{id:guid}/rate", async (Guid id, RateSupplierRequest req, ProcurementDbContext db) =>
        {
            var supplier = await db.Suppliers.FindAsync(id);
            if (supplier is null) return Results.NotFound();
            if (!Enum.TryParse<SupplierRating>(req.Rating, out var rating))
                return Results.BadRequest(new { error = "Invalid rating." });
            supplier.Rate(rating);
            await db.SaveChangesAsync();
            return Results.Ok(new { supplier.Id, Rating = supplier.Rating.ToString() });
        }).WithName("RateSupplier");

        // ═══════════ Purchase Requests ═══════════
        var pr = app.MapGroup("/api/procurement/requests").WithTags("Procurement - Purchase Requests");

        pr.MapGet("/", async (ProcurementDbContext db, string? status,
            int page = 1, int pageSize = 20) =>
        {
            var query = db.PurchaseRequests.AsQueryable();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PurchaseRequestStatus>(status, out var s))
                query = query.Where(r => r.Status == s);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(r => new { r.Id, r.RequestNumber, r.Department,
                    Status = r.Status.ToString(), r.EstimatedTotal, r.Currency,
                    r.RequiredByDate, r.CreatedAt })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListPurchaseRequests");

        pr.MapPost("/", async (CreatePurchaseRequestReq req, ProcurementDbContext db) =>
        {
            var request = PurchaseRequestBase.Create(req.RequestNumber, req.RequestedByUserId,
                req.Department, req.Description, req.ItemsJson,
                req.EstimatedTotal, req.Currency ?? "TRY", req.RequiredByDate);
            request.Submit();
            db.PurchaseRequests.Add(request);
            await db.SaveChangesAsync();
            return Results.Created($"/api/procurement/requests/{request.Id}",
                new { request.Id, request.RequestNumber, Status = request.Status.ToString() });
        }).WithName("CreatePurchaseRequest");

        pr.MapPost("/{id:guid}/approve", async (Guid id, ProcurementDbContext db) =>
        {
            var request = await db.PurchaseRequests.FindAsync(id);
            if (request is null) return Results.NotFound();
            request.Approve();
            await db.SaveChangesAsync();
            return Results.Ok(new { request.Id, Status = request.Status.ToString() });
        }).WithName("ApprovePurchaseRequest");

        pr.MapPost("/{id:guid}/reject", async (Guid id, ProcurementDbContext db) =>
        {
            var request = await db.PurchaseRequests.FindAsync(id);
            if (request is null) return Results.NotFound();
            request.Reject();
            await db.SaveChangesAsync();
            return Results.Ok(new { request.Id, Status = request.Status.ToString() });
        }).WithName("RejectPurchaseRequest");

        // ═══════════ Purchase Orders ═══════════
        var po = app.MapGroup("/api/procurement/orders").WithTags("Procurement - Purchase Orders");

        po.MapGet("/", async (ProcurementDbContext db, string? status, Guid? supplierId,
            int page = 1, int pageSize = 20) =>
        {
            var query = db.PurchaseOrders.Include(o => o.Supplier).AsQueryable();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PurchaseOrderStatus>(status, out var s))
                query = query.Where(o => o.Status == s);
            if (supplierId.HasValue) query = query.Where(o => o.SupplierId == supplierId.Value);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(o => new { o.Id, o.OrderNumber, SupplierName = o.Supplier.Name,
                    Status = o.Status.ToString(), o.OrderDate, o.GrandTotal,
                    o.ReceivedTotal, MatchingStatus = o.MatchingStatus.ToString() })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListPurchaseOrders");

        po.MapGet("/{id:guid}", async (Guid id, ProcurementDbContext db) =>
        {
            var o = await db.PurchaseOrders.Include(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.Id == id);
            return o is null ? Results.NotFound() : Results.Ok(o);
        }).WithName("GetPurchaseOrder");

        po.MapPost("/", async (CreatePurchaseOrderReq req, ProcurementDbContext db) =>
        {
            var po = PurchaseOrderBase.Create(req.OrderNumber, req.SupplierId,
                req.OrderDate == default ? DateTime.UtcNow : req.OrderDate,
                req.SupplierName, req.Currency ?? "TRY", req.ExpectedDeliveryDate,
                req.ItemsJson, req.SubTotal, req.TaxTotal, req.Notes);
            db.PurchaseOrders.Add(po);
            await db.SaveChangesAsync();
            return Results.Created($"/api/procurement/orders/{po.Id}",
                new { po.Id, po.OrderNumber, po.GrandTotal });
        }).WithName("CreatePurchaseOrder");

        po.MapPost("/{id:guid}/receive", async (Guid id, ReceiveRequest req, ProcurementDbContext db) =>
        {
            var order = await db.PurchaseOrders.FindAsync(id);
            if (order is null) return Results.NotFound();
            if (req.Full) order.ReceiveFull();
            else order.ReceivePartial(req.Amount);
            await db.SaveChangesAsync();
            return Results.Ok(new { order.Id, Status = order.Status.ToString(), order.ReceivedTotal });
        }).WithName("ReceivePurchaseOrder");

        po.MapPost("/{id:guid}/match-invoice", async (Guid id, MatchInvoiceRequest req, ProcurementDbContext db) =>
        {
            var order = await db.PurchaseOrders.FindAsync(id);
            if (order is null) return Results.NotFound();
            order.MatchInvoice(req.InvoiceId);
            await db.SaveChangesAsync();
            return Results.Ok(new { order.Id, MatchingStatus = order.MatchingStatus.ToString() });
        }).WithName("MatchInvoice").WithSummary("3-way matching: PO ↔ Teslim ↔ Fatura");

        return app;
    }
}

// ── DTOs ─────────────────────────────────────────────────
public sealed record CreateSupplierRequest(string Code, string Name,
    string? Email = null, string? Phone = null, string? Address = null,
    string? TaxNumber = null, string? ContactPerson = null, int PaymentTermDays = 30);

public sealed record RateSupplierRequest(string Rating);

public sealed record CreatePurchaseRequestReq(string RequestNumber, Guid RequestedByUserId,
    string? Department = null, string? Description = null, string? ItemsJson = null,
    decimal EstimatedTotal = 0, string? Currency = null, DateTime? RequiredByDate = null);

public sealed record CreatePurchaseOrderReq(string OrderNumber, Guid SupplierId,
    DateTime OrderDate = default, string? SupplierName = null, string? Currency = null,
    DateTime? ExpectedDeliveryDate = null, string? ItemsJson = null,
    decimal SubTotal = 0, decimal TaxTotal = 0, string? Notes = null);

public sealed record ReceiveRequest(bool Full = false, decimal Amount = 0);
public sealed record MatchInvoiceRequest(Guid InvoiceId);
