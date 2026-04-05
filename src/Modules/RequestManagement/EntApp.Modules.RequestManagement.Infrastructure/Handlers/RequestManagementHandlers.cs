using EntApp.Modules.RequestManagement.Application.Commands;
using EntApp.Modules.RequestManagement.Application.IntegrationEvents;
using EntApp.Modules.RequestManagement.Application.Queries;
using EntApp.Modules.RequestManagement.Domain.Entities;
using EntApp.Modules.RequestManagement.Domain.Enums;
using EntApp.Modules.RequestManagement.Domain.Ids;
using EntApp.Modules.RequestManagement.Infrastructure.Persistence;
using EntApp.Modules.RequestManagement.Infrastructure.Services;
using EntApp.Shared.Contracts.Identity;
using EntApp.Shared.Contracts.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.RequestManagement.Infrastructure.Handlers;

// ═══════════════════════════════════════════════════════════════
//  Department Handlers
// ═══════════════════════════════════════════════════════════════

public sealed class CreateDepartmentHandler(RequestManagementDbContext db)
    : IRequestHandler<CreateDepartmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateDepartmentCommand request, CancellationToken ct)
    {
        var dept = Department.Create(request.Name, request.Code, request.Description,
            request.ManagerUserId,
            request.ParentDepartmentId.HasValue ? new DepartmentId(request.ParentDepartmentId.Value) : null);

        db.Departments.Add(dept);
        await db.SaveChangesAsync(ct);
        return dept.Id.Value;
    }
}

public sealed class UpdateDepartmentHandler(RequestManagementDbContext db)
    : IRequestHandler<UpdateDepartmentCommand>
{
    public async Task Handle(UpdateDepartmentCommand request, CancellationToken ct)
    {
        var dept = await db.Departments.FindAsync([new DepartmentId(request.Id)], ct)
            ?? throw new KeyNotFoundException($"Department '{request.Id}' not found.");

        dept.Update(request.Name, request.Code, request.Description,
            request.ManagerUserId,
            request.ParentDepartmentId.HasValue ? new DepartmentId(request.ParentDepartmentId.Value) : null);

        await db.SaveChangesAsync(ct);
    }
}

// ═══════════════════════════════════════════════════════════════
//  RequestCategory Handlers
// ═══════════════════════════════════════════════════════════════

public sealed class CreateCategoryHandler(RequestManagementDbContext db)
    : IRequestHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var category = RequestCategory.Create(
            request.Name, request.Code, new DepartmentId(request.DepartmentId),
            request.Description,
            request.SlaDefinitionId.HasValue ? new SlaDefinitionId(request.SlaDefinitionId.Value) : null,
            request.WorkflowDefinitionId, request.FormSchemaJson, request.AutoProjectThreshold);

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return category.Id.Value;
    }
}

public sealed class UpdateCategoryHandler(RequestManagementDbContext db)
    : IRequestHandler<UpdateCategoryCommand>
{
    public async Task Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var cat = await db.Categories.FindAsync([new RequestCategoryId(request.Id)], ct)
            ?? throw new KeyNotFoundException($"Category '{request.Id}' not found.");

        cat.Update(request.Name, request.Code, new DepartmentId(request.DepartmentId),
            request.Description,
            request.SlaDefinitionId.HasValue ? new SlaDefinitionId(request.SlaDefinitionId.Value) : null,
            request.WorkflowDefinitionId, request.FormSchemaJson, request.AutoProjectThreshold);

        await db.SaveChangesAsync(ct);
    }
}

// ═══════════════════════════════════════════════════════════════
//  SlaDefinition Handlers
// ═══════════════════════════════════════════════════════════════

public sealed class CreateSlaHandler(RequestManagementDbContext db)
    : IRequestHandler<CreateSlaCommand, Guid>
{
    public async Task<Guid> Handle(CreateSlaCommand request, CancellationToken ct)
    {
        var sla = SlaDefinition.Create(request.Name, request.Description,
            request.ResponseTimeJson, request.ResolutionTimeJson);

        db.SlaDefinitions.Add(sla);
        await db.SaveChangesAsync(ct);
        return sla.Id.Value;
    }
}

public sealed class UpdateSlaHandler(RequestManagementDbContext db)
    : IRequestHandler<UpdateSlaCommand>
{
    public async Task Handle(UpdateSlaCommand request, CancellationToken ct)
    {
        var sla = await db.SlaDefinitions.FindAsync([new SlaDefinitionId(request.Id)], ct)
            ?? throw new KeyNotFoundException($"SLA '{request.Id}' not found.");

        sla.Update(request.Name, request.Description, request.ResponseTimeJson, request.ResolutionTimeJson);
        await db.SaveChangesAsync(ct);
    }
}

// ═══════════════════════════════════════════════════════════════
//  Ticket Handlers
// ═══════════════════════════════════════════════════════════════

public sealed class CreateTicketHandler(
    RequestManagementDbContext db, ICurrentUser currentUser, IEventBus eventBus)
    : IRequestHandler<CreateTicketCommand, Guid>
{
    public async Task<Guid> Handle(CreateTicketCommand request, CancellationToken ct)
    {
        var number = await TicketNumberGenerator.NextAsync(db, ct);

        var ticket = Ticket.Create(number, request.Title,
            new RequestCategoryId(request.CategoryId), new DepartmentId(request.DepartmentId),
            currentUser.UserId, request.Description, request.Priority, request.Channel);

        // SLA hesapla
        var category = await db.Categories
            .Include(c => c.SlaDefinitionEntity)
            .FirstOrDefaultAsync(c => c.Id == new RequestCategoryId(request.CategoryId), ct);

        if (category?.SlaDefinitionEntity is not null)
        {
            var sla = category.SlaDefinitionEntity;
            ticket.SetSlaDeadlines(
                SlaCalculator.CalculateResponseDeadline(sla.ResponseTimeJson, request.Priority),
                SlaCalculator.CalculateResolutionDeadline(sla.ResolutionTimeJson, request.Priority));
        }

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);

        // Integration event
        await eventBus.PublishAsync(new TicketCreatedEvent(
            ticket.Id.Value, ticket.Number, ticket.Title,
            request.CategoryId, request.DepartmentId,
            currentUser.UserId, request.Priority.ToString(), request.Channel.ToString()), ct);

        return ticket.Id.Value;
    }
}

public sealed class UpdateTicketHandler(RequestManagementDbContext db)
    : IRequestHandler<UpdateTicketCommand>
{
    public async Task Handle(UpdateTicketCommand request, CancellationToken ct)
    {
        var ticket = await db.Tickets.FindAsync([new TicketId(request.Id)], ct)
            ?? throw new KeyNotFoundException($"Ticket '{request.Id}' not found.");

        ticket.Update(request.Title, request.Description, request.Priority);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class AssignTicketHandler(
    RequestManagementDbContext db, IEventBus eventBus)
    : IRequestHandler<AssignTicketCommand>
{
    public async Task Handle(AssignTicketCommand request, CancellationToken ct)
    {
        var ticket = await db.Tickets.FindAsync([new TicketId(request.TicketId)], ct)
            ?? throw new KeyNotFoundException($"Ticket '{request.TicketId}' not found.");

        var previousAssignee = ticket.AssigneeUserId;
        ticket.Assign(request.AssigneeUserId);
        await db.SaveChangesAsync(ct);

        await eventBus.PublishAsync(new TicketAssignedEvent(
            ticket.Id.Value, ticket.Number, request.AssigneeUserId, previousAssignee), ct);
    }
}

public sealed class ChangeTicketStatusHandler(
    RequestManagementDbContext db, ICurrentUser currentUser, IEventBus eventBus)
    : IRequestHandler<ChangeTicketStatusCommand>
{
    public async Task Handle(ChangeTicketStatusCommand request, CancellationToken ct)
    {
        var ticket = await db.Tickets
            .Include(t => t.StatusHistory)
            .FirstOrDefaultAsync(t => t.Id == new TicketId(request.TicketId), ct)
            ?? throw new KeyNotFoundException($"Ticket '{request.TicketId}' not found.");

        ticket.ChangeStatus(request.NewStatus, currentUser.UserId, request.Reason);
        await db.SaveChangesAsync(ct);

        if (request.NewStatus == TicketStatus.Resolved)
        {
            await eventBus.PublishAsync(new TicketResolvedEvent(
                ticket.Id.Value, ticket.Number,
                ticket.ReporterUserId, ticket.AssigneeUserId,
                ticket.ResolvedAt ?? DateTime.UtcNow), ct);
        }
    }
}

public sealed class CloseTicketHandler(RequestManagementDbContext db, ICurrentUser currentUser)
    : IRequestHandler<CloseTicketCommand>
{
    public async Task Handle(CloseTicketCommand request, CancellationToken ct)
    {
        var ticket = await db.Tickets
            .Include(t => t.StatusHistory)
            .FirstOrDefaultAsync(t => t.Id == new TicketId(request.TicketId), ct)
            ?? throw new KeyNotFoundException($"Ticket '{request.TicketId}' not found.");

        ticket.ChangeStatus(TicketStatus.Closed, currentUser.UserId, request.Reason);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class AddCommentHandler(RequestManagementDbContext db, ICurrentUser currentUser)
    : IRequestHandler<AddCommentCommand, Guid>
{
    public async Task<Guid> Handle(AddCommentCommand request, CancellationToken ct)
    {
        _ = await db.Tickets.FindAsync([new TicketId(request.TicketId)], ct)
            ?? throw new KeyNotFoundException($"Ticket '{request.TicketId}' not found.");

        var comment = TicketComment.Create(
            new TicketId(request.TicketId), request.Content, currentUser.UserId, request.IsInternal);

        db.TicketComments.Add(comment);
        await db.SaveChangesAsync(ct);
        return comment.Id.Value;
    }
}

// ═══════════════════════════════════════════════════════════════
//  Query Handlers
// ═══════════════════════════════════════════════════════════════

public sealed class ListDepartmentsHandler(RequestManagementDbContext db)
    : IRequestHandler<ListDepartmentsQuery, IReadOnlyList<Department>>
{
    public async Task<IReadOnlyList<Department>> Handle(ListDepartmentsQuery request, CancellationToken ct)
    {
        var query = db.Departments.AsQueryable();
        if (request.ActiveOnly == true) query = query.Where(d => d.IsActive);
        return await query.OrderBy(d => d.Name).ToListAsync(ct);
    }
}

public sealed class GetDepartmentHandler(RequestManagementDbContext db)
    : IRequestHandler<GetDepartmentQuery, Department?>
{
    public async Task<Department?> Handle(GetDepartmentQuery request, CancellationToken ct)
    {
        return await db.Departments
            .Include(d => d.Categories)
            .Include(d => d.SubDepartments)
            .FirstOrDefaultAsync(d => d.Id == new DepartmentId(request.Id), ct);
    }
}

public sealed class ListCategoriesHandler(RequestManagementDbContext db)
    : IRequestHandler<ListCategoriesQuery, IReadOnlyList<RequestCategory>>
{
    public async Task<IReadOnlyList<RequestCategory>> Handle(ListCategoriesQuery request, CancellationToken ct)
    {
        var query = db.Categories.Include(c => c.Department).AsQueryable();
        if (request.DepartmentId.HasValue)
            query = query.Where(c => c.DepartmentId == new DepartmentId(request.DepartmentId.Value));
        if (request.ActiveOnly == true) query = query.Where(c => c.IsActive);
        return await query.OrderBy(c => c.Name).ToListAsync(ct);
    }
}

public sealed class GetCategoryHandler(RequestManagementDbContext db)
    : IRequestHandler<GetCategoryQuery, RequestCategory?>
{
    public async Task<RequestCategory?> Handle(GetCategoryQuery request, CancellationToken ct)
    {
        return await db.Categories
            .Include(c => c.Department)
            .Include(c => c.SlaDefinitionEntity)
            .FirstOrDefaultAsync(c => c.Id == new RequestCategoryId(request.Id), ct);
    }
}

public sealed class ListSlaDefinitionsHandler(RequestManagementDbContext db)
    : IRequestHandler<ListSlaDefinitionsQuery, IReadOnlyList<SlaDefinition>>
{
    public async Task<IReadOnlyList<SlaDefinition>> Handle(ListSlaDefinitionsQuery request, CancellationToken ct)
    {
        var query = db.SlaDefinitions.AsQueryable();
        if (request.ActiveOnly == true) query = query.Where(s => s.IsActive);
        return await query.OrderBy(s => s.Name).ToListAsync(ct);
    }
}

public sealed class ListTicketsHandler(RequestManagementDbContext db)
    : IRequestHandler<ListTicketsQuery, TicketListResult>
{
    public async Task<TicketListResult> Handle(ListTicketsQuery request, CancellationToken ct)
    {
        var query = db.Tickets
            .Include(t => t.Category)
            .Include(t => t.Department)
            .AsQueryable();

        if (request.Status.HasValue) query = query.Where(t => t.Status == request.Status.Value);
        if (request.Priority.HasValue) query = query.Where(t => t.Priority == request.Priority.Value);
        if (request.AssigneeUserId.HasValue) query = query.Where(t => t.AssigneeUserId == request.AssigneeUserId.Value);
        if (request.DepartmentId.HasValue) query = query.Where(t => t.DepartmentId == new DepartmentId(request.DepartmentId.Value));

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new TicketListResult(items, totalCount);
    }
}

public sealed class GetTicketHandler(RequestManagementDbContext db)
    : IRequestHandler<GetTicketQuery, Ticket?>
{
    public async Task<Ticket?> Handle(GetTicketQuery request, CancellationToken ct)
    {
        return await db.Tickets
            .Include(t => t.Category)
            .Include(t => t.Department)
            .Include(t => t.Comments.OrderByDescending(c => c.CreatedAt))
            .Include(t => t.StatusHistory.OrderByDescending(h => h.ChangedAt))
            .FirstOrDefaultAsync(t => t.Id == new TicketId(request.Id), ct);
    }
}

public sealed class GetMyTicketsHandler(RequestManagementDbContext db)
    : IRequestHandler<GetMyTicketsQuery, TicketListResult>
{
    public async Task<TicketListResult> Handle(GetMyTicketsQuery request, CancellationToken ct)
    {
        var query = db.Tickets
            .Include(t => t.Category)
            .Where(t => t.ReporterUserId == request.ReporterUserId);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new TicketListResult(items, totalCount);
    }
}
