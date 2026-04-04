using EntApp.Modules.CRM.Domain.Entities;
using EntApp.Modules.CRM.Domain.Enums;
using EntApp.Modules.CRM.Application.IntegrationEvents;
using EntApp.Modules.CRM.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.CRM.Infrastructure.Endpoints;

/// <summary>CRM REST API endpoint'leri.</summary>
public static class CrmEndpoints
{
    public static IEndpointRouteBuilder MapCrmEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════ Customers ═══════════
        var customers = app.MapGroup("/api/crm/customers").WithTags("CRM - Customers");

        customers.MapGet("/", async (CrmDbContext db, string? search, string? segment,
            int page = 1, int pageSize = 20) =>
        {
            var query = db.Customers.Where(c => c.IsActive);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.Name.Contains(search) || (c.Code != null && c.Code.Contains(search)));
            if (!string.IsNullOrEmpty(segment) && Enum.TryParse<CustomerSegment>(segment, out var seg))
                query = query.Where(c => c.Segment == seg);

            var total = await query.CountAsync();
            var items = await query.OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(c => new { c.Id, c.Name, c.Code, c.Email, c.Phone, c.City, c.Country,
                    CustomerType = c.CustomerType.ToString(), Segment = c.Segment.ToString(), c.CreatedAt })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListCustomers");

        customers.MapGet("/{id:guid}", async (Guid id, CrmDbContext db) =>
        {
            var c = await db.Customers.Include(x => x.Contacts).Include(x => x.Opportunities)
                .FirstOrDefaultAsync(x => x.Id == id);
            return c is null ? Results.NotFound() : Results.Ok(c);
        }).WithName("GetCustomer");

        customers.MapPost("/", async (CreateCustomerRequest req, CrmDbContext db, IEventBus eventBus) =>
        {
            Enum.TryParse<CustomerType>(req.CustomerType, out var type);
            Enum.TryParse<CustomerSegment>(req.Segment, out var segment);
            var customer = CustomerBase.Create(req.Name, type, req.Code, req.Email,
                req.Phone, req.Address, req.City, req.Country, req.TaxNumber, segment);
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            await eventBus.PublishAsync(new CustomerCreatedEvent(
                customer.Id, customer.Name, customer.CustomerType.ToString(),
                customer.Email, customer.TaxNumber));

            return Results.Created($"/api/crm/customers/{customer.Id}", new { customer.Id, customer.Name });
        }).WithName("CreateCustomer");

        // ═══════════ Contacts ═══════════
        var contacts = app.MapGroup("/api/crm/contacts").WithTags("CRM - Contacts");

        contacts.MapGet("/", async (CrmDbContext db, Guid? customerId, int page = 1, int pageSize = 20) =>
        {
            var query = db.Contacts.AsQueryable();
            if (customerId.HasValue) query = query.Where(c => c.CustomerId == customerId.Value);

            var total = await query.CountAsync();
            var items = await query.OrderBy(c => c.LastName)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(c => new { c.Id, c.CustomerId, c.FirstName, c.LastName, c.Title, c.Email, c.Phone, c.IsPrimary })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListContacts");

        contacts.MapPost("/", async (CreateContactRequest req, CrmDbContext db) =>
        {
            var contact = ContactBase.Create(req.CustomerId, req.FirstName, req.LastName,
                req.Title, req.Email, req.Phone, req.Department, req.IsPrimary);
            db.Contacts.Add(contact);
            await db.SaveChangesAsync();
            return Results.Created($"/api/crm/contacts/{contact.Id}", new { contact.Id });
        }).WithName("CreateContact");

        // ═══════════ Opportunities ═══════════
        var opps = app.MapGroup("/api/crm/opportunities").WithTags("CRM - Opportunities");

        opps.MapGet("/", async (CrmDbContext db, string? stage, Guid? customerId,
            int page = 1, int pageSize = 20) =>
        {
            var query = db.Opportunities.Include(o => o.Customer).AsQueryable();
            if (!string.IsNullOrEmpty(stage) && Enum.TryParse<OpportunityStage>(stage, out var s))
                query = query.Where(o => o.Stage == s);
            if (customerId.HasValue) query = query.Where(o => o.CustomerId == customerId.Value);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(o => new { o.Id, o.Title, CustomerName = o.Customer.Name,
                    o.EstimatedValue, o.Currency, Stage = o.Stage.ToString(),
                    o.Probability, o.ExpectedCloseDate, o.CreatedAt })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListOpportunities");

        opps.MapPost("/", async (CreateOpportunityRequest req, CrmDbContext db) =>
        {
            Enum.TryParse<OpportunityStage>(req.Stage, out var stage);
            var opp = OpportunityBase.Create(req.CustomerId, req.Title, req.EstimatedValue,
                req.Currency ?? "TRY", stage, req.Description, req.ExpectedCloseDate, req.AssignedUserId);
            db.Opportunities.Add(opp);
            await db.SaveChangesAsync();
            return Results.Created($"/api/crm/opportunities/{opp.Id}", new { opp.Id, opp.Title });
        }).WithName("CreateOpportunity");

        opps.MapPost("/{id:guid}/advance", async (Guid id, AdvanceStageRequest req, CrmDbContext db, IEventBus eventBus) =>
        {
            var opp = await db.Opportunities.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == id);
            if (opp is null) return Results.NotFound();
            if (!Enum.TryParse<OpportunityStage>(req.Stage, out var stage))
                return Results.BadRequest(new { error = "Invalid stage." });
            opp.AdvanceStage(stage);
            await db.SaveChangesAsync();

            if (stage == OpportunityStage.ClosedWon)
                await eventBus.PublishAsync(new OpportunityWonEvent(
                    opp.Id, opp.Title, opp.CustomerId, opp.Customer.Name,
                    opp.EstimatedValue, opp.Currency));
            else if (stage == OpportunityStage.ClosedLost)
                await eventBus.PublishAsync(new OpportunityLostEvent(
                    opp.Id, opp.Title, opp.CustomerId, opp.LostReason));

            return Results.Ok(new { opp.Id, Stage = opp.Stage.ToString(), opp.Probability });
        }).WithName("AdvanceOpportunityStage");

        // ═══════════ Pipeline Summary ═══════════
        opps.MapGet("/pipeline", async (CrmDbContext db) =>
        {
            var pipeline = await db.Opportunities
                .GroupBy(o => o.Stage)
                .Select(g => new { Stage = g.Key.ToString(), Count = g.Count(),
                    TotalValue = g.Sum(o => o.EstimatedValue) })
                .OrderBy(x => x.Stage)
                .ToListAsync();
            return Results.Ok(pipeline);
        }).WithName("OpportunityPipeline").WithSummary("Fırsat pipeline özeti");

        // ═══════════ Activities ═══════════
        var acts = app.MapGroup("/api/crm/activities").WithTags("CRM - Activities");

        acts.MapGet("/", async (CrmDbContext db, Guid? customerId, string? type,
            int page = 1, int pageSize = 20) =>
        {
            var query = db.Activities.AsQueryable();
            if (customerId.HasValue) query = query.Where(a => a.CustomerId == customerId.Value);
            if (!string.IsNullOrEmpty(type) && Enum.TryParse<ActivityType>(type, out var at))
                query = query.Where(a => a.ActivityType == at);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .Select(a => new { a.Id, a.Subject, ActivityType = a.ActivityType.ToString(),
                    Status = a.Status.ToString(), a.CustomerId, a.DueDate, a.CreatedAt })
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, pageNumber = page, pageSize });
        }).WithName("ListActivities");

        acts.MapPost("/", async (CreateActivityRequest req, CrmDbContext db) =>
        {
            Enum.TryParse<ActivityType>(req.ActivityType, out var type);
            var activity = ActivityBase.Create(req.Subject, type, req.CustomerId,
                req.OpportunityId, req.Description, req.DueDate, req.AssignedUserId);
            db.Activities.Add(activity);
            await db.SaveChangesAsync();
            return Results.Created($"/api/crm/activities/{activity.Id}", new { activity.Id });
        }).WithName("CreateActivity");

        return app;
    }
}

// ── DTOs ─────────────────────────────────────────────────
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
