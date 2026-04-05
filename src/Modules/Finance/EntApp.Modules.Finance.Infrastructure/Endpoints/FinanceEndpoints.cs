using EntApp.Modules.Finance.Application.Commands;
using EntApp.Modules.Finance.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Modules.Finance.Infrastructure.Endpoints;

/// <summary>Finance REST API endpoint'leri — CQRS/MediatR ile.</summary>
public static class FinanceEndpoints
{
    public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════ Accounts ═══════════
        var accs = app.MapGroup("/api/finance/accounts").WithTags("Finance - Accounts");

        accs.MapGet("/", async (ISender mediator, string? search, string? type,
            int page = 1, int pageSize = 20) =>
        {
            var result = await mediator.Send(new ListAccountsQuery(search, type, page, pageSize));
            return Results.Ok(result);
        }).WithName("ListAccounts");

        accs.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var result = await mediator.Send(new GetAccountQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithName("GetAccount");

        accs.MapPost("/", async (CreateAccountRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateAccountCommand(
                req.Code, req.Name, req.AccountType, req.Currency,
                req.TaxNumber, req.Email, req.Phone, req.Address));
            return Results.Created($"/api/finance/accounts/{id}", new { id });
        }).WithName("CreateAccount");

        accs.MapGet("/balance-summary", async (ISender mediator) =>
        {
            var result = await mediator.Send(new GetBalanceSummaryQuery());
            return Results.Ok(result);
        }).WithName("AccountBalanceSummary").WithSummary("Hesap tipi bazlı bakiye özeti");

        // ═══════════ Invoices ═══════════
        var inv = app.MapGroup("/api/finance/invoices").WithTags("Finance - Invoices");

        inv.MapGet("/", async (ISender mediator, string? type, string? status,
            int page = 1, int pageSize = 20) =>
        {
            var result = await mediator.Send(new ListInvoicesQuery(type, status, page, pageSize));
            return Results.Ok(result);
        }).WithName("ListInvoices");

        inv.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var result = await mediator.Send(new GetInvoiceQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithName("GetInvoice");

        inv.MapPost("/", async (CreateInvoiceRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new CreateInvoiceCommand(
                req.InvoiceNumber, req.AccountId, req.InvoiceType,
                req.InvoiceDate, req.DueDate, req.Currency, req.Notes,
                req.Items?.Select(i => new CreateInvoiceItemDto(
                    i.Description, i.Quantity, i.UnitPrice, i.TaxRate, i.DiscountRate)).ToList()));
            return Results.Created($"/api/finance/invoices/{result.Id}", result);
        }).WithName("CreateInvoice");

        inv.MapPost("/{id:guid}/approve", async (Guid id, ISender mediator) =>
        {
            var status = await mediator.Send(new ApproveInvoiceCommand(id));
            return Results.Ok(new { id, status });
        }).WithName("ApproveInvoice");

        inv.MapGet("/overdue", async (ISender mediator) =>
        {
            var result = await mediator.Send(new GetOverdueInvoicesQuery());
            return Results.Ok(result);
        }).WithName("OverdueInvoices").WithSummary("Vadesi geçmiş faturalar");

        // ═══════════ Payments ═══════════
        var pay = app.MapGroup("/api/finance/payments").WithTags("Finance - Payments");

        pay.MapGet("/", async (ISender mediator, Guid? accountId, string? direction,
            int page = 1, int pageSize = 20) =>
        {
            var result = await mediator.Send(new ListPaymentsQuery(accountId, direction, page, pageSize));
            return Results.Ok(result);
        }).WithName("ListPayments");

        pay.MapPost("/", async (CreatePaymentRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreatePaymentCommand(
                req.AccountId, req.Amount, req.Direction, req.Method,
                req.PaymentDate, req.InvoiceId, req.Currency, req.ReferenceNumber, req.Notes));
            return Results.Created($"/api/finance/payments/{id}", new { id });
        }).WithName("CreatePayment");

        return app;
    }
}

// ── Request DTO'lar ─────────────────────────────────────────
public sealed record CreateAccountRequest(string Code, string Name, string AccountType = "Customer",
    string? Currency = null, string? TaxNumber = null, string? Email = null,
    string? Phone = null, string? Address = null);

public sealed record CreateInvoiceRequest(string InvoiceNumber, Guid AccountId,
    string InvoiceType = "Sales", DateTime InvoiceDate = default, DateTime DueDate = default,
    string? Currency = null, string? Notes = null, List<CreateInvoiceItemRequest>? Items = null);

public sealed record CreateInvoiceItemRequest(string Description, decimal Quantity = 1,
    decimal UnitPrice = 0, decimal TaxRate = 20, decimal DiscountRate = 0);

public sealed record CreatePaymentRequest(Guid AccountId, decimal Amount,
    string Direction = "Incoming", string Method = "BankTransfer",
    DateTime? PaymentDate = null, Guid? InvoiceId = null,
    string? Currency = null, string? ReferenceNumber = null, string? Notes = null);
