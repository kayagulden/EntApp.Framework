using EntApp.Modules.CRM.Application.Commands;
using EntApp.Modules.CRM.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Modules.CRM.Infrastructure.Endpoints;

/// <summary>CRM REST API endpoint'leri — CQRS/MediatR ile.</summary>
public static class CrmEndpoints
{
    public static IEndpointRouteBuilder MapCrmEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════ Customers ═══════════
        var customers = app.MapGroup("/api/crm/customers").WithTags("CRM - Customers");

        customers.MapGet("/", async (ISender mediator, string? search, string? segment,
            int page = 1, int pageSize = 20) =>
        {
            var result = await mediator.Send(new ListCustomersQuery(search, segment, page, pageSize));
            return Results.Ok(result);
        }).WithName("ListCustomers");

        customers.MapGet("/{id:guid}", async (Guid id, ISender mediator) =>
        {
            var result = await mediator.Send(new GetCustomerQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithName("GetCustomer");

        customers.MapPost("/", async (CreateCustomerRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateCustomerCommand(
                req.Name, req.Code, req.Email, req.Phone, req.Address,
                req.City, req.Country, req.TaxNumber, req.CustomerType, req.Segment));
            return Results.Created($"/api/crm/customers/{id}", new { id });
        }).WithName("CreateCustomer");

        // ═══════════ Contacts ═══════════
        var contacts = app.MapGroup("/api/crm/contacts").WithTags("CRM - Contacts");

        contacts.MapGet("/", async (ISender mediator, Guid? customerId, int page = 1, int pageSize = 20) =>
        {
            var result = await mediator.Send(new ListContactsQuery(customerId, page, pageSize));
            return Results.Ok(result);
        }).WithName("ListContacts");

        contacts.MapPost("/", async (CreateContactRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateContactCommand(
                req.CustomerId, req.FirstName, req.LastName,
                req.Title, req.Email, req.Phone, req.Department, req.IsPrimary));
            return Results.Created($"/api/crm/contacts/{id}", new { id });
        }).WithName("CreateContact");

        // ═══════════ Opportunities ═══════════
        var opps = app.MapGroup("/api/crm/opportunities").WithTags("CRM - Opportunities");

        opps.MapGet("/", async (ISender mediator, string? stage, Guid? customerId,
            int page = 1, int pageSize = 20) =>
        {
            var result = await mediator.Send(new ListOpportunitiesQuery(stage, customerId, page, pageSize));
            return Results.Ok(result);
        }).WithName("ListOpportunities");

        opps.MapPost("/", async (CreateOpportunityRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateOpportunityCommand(
                req.CustomerId, req.Title, req.EstimatedValue, req.Currency,
                req.Stage, req.Description, req.ExpectedCloseDate, req.AssignedUserId));
            return Results.Created($"/api/crm/opportunities/{id}", new { id });
        }).WithName("CreateOpportunity");

        opps.MapPost("/{id:guid}/advance", async (Guid id, AdvanceStageRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new AdvanceOpportunityStageCommand(id, req.Stage));
            return Results.Ok(result);
        }).WithName("AdvanceOpportunityStage");

        opps.MapGet("/pipeline", async (ISender mediator) =>
        {
            var result = await mediator.Send(new GetPipelineQuery());
            return Results.Ok(result);
        }).WithName("OpportunityPipeline").WithSummary("Fırsat pipeline özeti");

        // ═══════════ Activities ═══════════
        var acts = app.MapGroup("/api/crm/activities").WithTags("CRM - Activities");

        acts.MapGet("/", async (ISender mediator, Guid? customerId, string? type,
            int page = 1, int pageSize = 20) =>
        {
            var result = await mediator.Send(new ListActivitiesQuery(customerId, type, page, pageSize));
            return Results.Ok(result);
        }).WithName("ListActivities");

        acts.MapPost("/", async (CreateActivityRequest req, ISender mediator) =>
        {
            var id = await mediator.Send(new CreateActivityCommand(
                req.Subject, req.ActivityType, req.CustomerId, req.OpportunityId,
                req.Description, req.DueDate, req.AssignedUserId));
            return Results.Created($"/api/crm/activities/{id}", new { id });
        }).WithName("CreateActivity");

        return app;
    }
}

// ── Request DTO'lar ─────────────────────────────────────────
public sealed record CreateCustomerRequest(string Name, string? Code = null, string? Email = null,
    string? Phone = null, string? Address = null, string? City = null, string? Country = null,
    string? TaxNumber = null, string CustomerType = "Company", string Segment = "Standard");

public sealed record CreateContactRequest(Guid CustomerId, string FirstName, string LastName,
    string? Title = null, string? Email = null, string? Phone = null,
    string? Department = null, bool IsPrimary = false);

public sealed record CreateOpportunityRequest(Guid CustomerId, string Title,
    decimal EstimatedValue = 0, string? Currency = null, string Stage = "Lead",
    string? Description = null, DateTime? ExpectedCloseDate = null, Guid? AssignedUserId = null);

public sealed record AdvanceStageRequest(string Stage);

public sealed record CreateActivityRequest(string Subject, string ActivityType = "Note",
    Guid? CustomerId = null, Guid? OpportunityId = null,
    string? Description = null, DateTime? DueDate = null, Guid? AssignedUserId = null);
