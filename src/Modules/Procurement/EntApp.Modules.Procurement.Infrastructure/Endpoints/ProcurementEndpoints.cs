using EntApp.Modules.Procurement.Application.Commands;
using EntApp.Modules.Procurement.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Modules.Procurement.Infrastructure.Endpoints;

/// <summary>Procurement REST API endpoint'leri — CQRS/MediatR ile.</summary>
public static class ProcurementEndpoints
{
    public static IEndpointRouteBuilder MapProcurementEndpoints(this IEndpointRouteBuilder app)
    {
        var sup = app.MapGroup("/api/procurement/suppliers").WithTags("Procurement - Suppliers");
        sup.MapGet("/", async (ISender mediator, string? search, string? rating, int page = 1, int pageSize = 20)
            => Results.Ok(await mediator.Send(new ListSuppliersQuery(search, rating, page, pageSize)))).WithName("ListSuppliers");
        sup.MapPost("/", async (CreateSupplierRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateSupplierCommand(req.Code, req.Name, req.Email, req.Phone,
                req.Address, req.TaxNumber, req.ContactPerson, req.PaymentTermDays));
            return Results.Created($"/api/procurement/suppliers/{id}", new { id });
        }).WithName("CreateSupplier");
        sup.MapPost("/{id:guid}/rate", async (Guid id, RateSupplierRequest req, ISender mediator) =>
        {
            var rating = await mediator.Send(new RateSupplierCommand(id, req.Rating));
            return Results.Ok(new { id, rating });
        }).WithName("RateSupplier");

        var pr = app.MapGroup("/api/procurement/requests").WithTags("Procurement - Purchase Requests");
        pr.MapGet("/", async (ISender mediator, string? status, int page = 1, int pageSize = 20)
            => Results.Ok(await mediator.Send(new ListPurchaseRequestsQuery(status, page, pageSize)))).WithName("ListPurchaseRequests");
        pr.MapPost("/", async (CreatePurchaseRequestReq req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreatePurchaseRequestCommand(req.RequestNumber, req.RequestedByUserId,
                req.Department, req.Description, req.ItemsJson, req.EstimatedTotal, req.Currency, req.RequiredByDate));
            return Results.Created($"/api/procurement/requests/{id}", new { id });
        }).WithName("CreatePurchaseRequest");
        pr.MapPost("/{id:guid}/approve", async (Guid id, ISender mediator)
            => Results.Ok(new { id, status = await mediator.Send(new ApprovePurchaseRequestCommand(id)) })).WithName("ApprovePurchaseRequest");
        pr.MapPost("/{id:guid}/reject", async (Guid id, ISender mediator)
            => Results.Ok(new { id, status = await mediator.Send(new RejectPurchaseRequestCommand(id)) })).WithName("RejectPurchaseRequest");

        var po = app.MapGroup("/api/procurement/orders").WithTags("Procurement - Purchase Orders");
        po.MapGet("/", async (ISender mediator, string? status, Guid? supplierId, int page = 1, int pageSize = 20)
            => Results.Ok(await mediator.Send(new ListPurchaseOrdersQuery(status, supplierId, page, pageSize)))).WithName("ListPurchaseOrders");
        po.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        { var r = await mediator.Send(new GetPurchaseOrderQuery(id)); return r is null ? Results.NotFound() : Results.Ok(r); }).WithName("GetPurchaseOrder");
        po.MapPost("/", async (CreatePurchaseOrderReq req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreatePurchaseOrderCommand(req.OrderNumber, req.SupplierId,
                req.OrderDate, req.SupplierName, req.Currency, req.ExpectedDeliveryDate,
                req.ItemsJson, req.SubTotal, req.TaxTotal, req.Notes));
            return Results.Created($"/api/procurement/orders/{id}", new { id });
        }).WithName("CreatePurchaseOrder");
        po.MapPost("/{id:guid}/receive", async (Guid id, ReceiveRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new ReceivePurchaseOrderCommand(id, req.Full, req.Amount));
            return Results.Ok(result);
        }).WithName("ReceivePurchaseOrder");
        po.MapPost("/{id:guid}/match-invoice", async (Guid id, MatchInvoiceRequest req, ISender mediator) =>
        {
            var status = await mediator.Send(new MatchInvoiceCommand(id, req.InvoiceId));
            return Results.Ok(new { id, matchingStatus = status });
        }).WithName("MatchInvoice").WithSummary("3-way matching: PO ↔ Teslim ↔ Fatura");

        return app;
    }
}

// ── Request DTO'lar ─────────────────────────────────────────
public sealed record CreateSupplierRequest(string Code, string Name, string? Email = null,
    string? Phone = null, string? Address = null, string? TaxNumber = null,
    string? ContactPerson = null, int PaymentTermDays = 30);
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
