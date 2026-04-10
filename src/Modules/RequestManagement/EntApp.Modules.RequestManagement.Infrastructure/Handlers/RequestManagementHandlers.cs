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
using EntApp.Shared.Kernel.Domain.Entities;
using EntApp.Shared.Kernel.Domain.Ids;
using Elsa.Common.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace EntApp.Modules.RequestManagement.Infrastructure.Handlers;

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
            request.WorkflowDefinitionId, request.FormSchemaJson, request.AutoProjectThreshold,
            request.DefaultQueueId.HasValue ? new ServiceQueueId(request.DefaultQueueId.Value) : null);

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
            request.WorkflowDefinitionId, request.FormSchemaJson, request.AutoProjectThreshold,
            request.DefaultQueueId.HasValue ? new ServiceQueueId(request.DefaultQueueId.Value) : null);

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
    RequestManagementDbContext db, ICurrentUser currentUser, IEventBus eventBus,
    IWorkflowStarter workflowStarter, ILogger<CreateTicketHandler> logger)
    : IRequestHandler<CreateTicketCommand, Guid>
{
    public async Task<Guid> Handle(CreateTicketCommand request, CancellationToken ct)
    {
        var number = await TicketNumberGenerator.NextAsync(db, ct);

        // Kategori ve departmanı yükle (SLA için)
        var category = await db.Categories
            .Include(c => c.SlaDefinitionEntity)
            .FirstOrDefaultAsync(c => c.Id == new RequestCategoryId(request.CategoryId), ct);

        var deptId = new DepartmentId(request.DepartmentId);

        // Ticket Unrouted olarak oluştur — routing workflow tarafından yapılacak
        var reporterUserId = request.ReporterUserId ?? currentUser.UserId;
        var ticket = Ticket.Create(number, request.Title,
            new RequestCategoryId(request.CategoryId), deptId,
            reporterUserId, request.Description, request.Priority, request.Channel,
            request.FormDataJson);

        // SLA hesapla
        if (category?.SlaDefinitionEntity is not null)
        {
            var sla = category.SlaDefinitionEntity;
            ticket.SetSlaDeadlines(
                SlaCalculator.CalculateResponseDeadline(sla.ResponseTimeJson, request.Priority),
                SlaCalculator.CalculateResolutionDeadline(sla.ResolutionTimeJson, request.Priority));
        }

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);

        // ── Workflow başlat ──────────────────────────────────
        if (category?.WorkflowDefinitionId is not null)
        {
            try
            {
                var response = await workflowStarter.StartWorkflowAsync(new StartWorkflowRequest
                {
                    WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(
                        category.WorkflowDefinitionId,
                        VersionOptions.Published),
                    Input = new Dictionary<string, object>
                    {
                        ["TicketId"] = ticket.Id.Value,
                        ["CategoryId"] = request.CategoryId,
                        ["DepartmentId"] = request.DepartmentId,
                        ["Priority"] = request.Priority.ToString(),
                        ["Channel"] = request.Channel.ToString()
                    },
                    CorrelationId = ticket.Id.Value.ToString()
                }, ct);

                if (response.WorkflowInstanceId is not null)
                {
                    ticket.LinkWorkflow(Guid.Parse(response.WorkflowInstanceId));
                    await db.SaveChangesAsync(ct);
                    logger.LogInformation(
                        "Workflow started for ticket {TicketNumber} (instance: {WorkflowInstanceId})",
                        number, response.WorkflowInstanceId);
                }
                else
                {
                    logger.LogWarning(
                        "Workflow could not be started for ticket {TicketNumber} (DefinitionId: {DefId})",
                        number, category.WorkflowDefinitionId);
                }
            }
            catch (Exception ex)
            {
                // Workflow başlatma hatası ticket oluşturmayı engellememeli
                logger.LogError(ex, "Failed to start workflow for ticket {TicketNumber}", number);
            }
        }
        else
        {
            // Migrasyon dönemi fallback — workflow'suz kategori için DefaultQueueId kullan
            if (category?.DefaultQueueId is not null)
            {
                ticket.RouteToQueue(category.DefaultQueueId.Value, TicketRoutingSource.CategoryDefault);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                logger.LogWarning(
                    "Ticket {TicketNumber} has no workflow and no default queue — remains Unrouted.", number);
            }
        }

        // Integration event
        await eventBus.PublishAsync(new TicketCreatedEvent(
            ticket.Id.Value, ticket.Number, ticket.Title,
            request.CategoryId, request.DepartmentId,
            ticket.ServiceQueueId?.Value,
            currentUser.UserId, request.Priority.ToString(), request.Channel.ToString(),
            ticket.RoutingSource.ToString()), ct);

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

public sealed class RouteTicketToQueueHandler(RequestManagementDbContext db)
    : IRequestHandler<RouteTicketToQueueCommand>
{
    public async Task Handle(RouteTicketToQueueCommand request, CancellationToken ct)
    {
        var ticket = await db.Tickets.FindAsync([new TicketId(request.TicketId)], ct)
            ?? throw new KeyNotFoundException($"Ticket '{request.TicketId}' not found.");

        var queue = await db.ServiceQueues.FindAsync([new ServiceQueueId(request.QueueId)], ct)
            ?? throw new KeyNotFoundException($"ServiceQueue '{request.QueueId}' not found.");

        ticket.RouteToQueue(queue.Id, TicketRoutingSource.Manual);
        await db.SaveChangesAsync(ct);
    }
}

// ═══════════════════════════════════════════════════════════════
//  Query Handlers
// ═══════════════════════════════════════════════════════════════

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
            .Include(t => t.ServiceQueue)
            .AsQueryable();

        if (request.Status.HasValue) query = query.Where(t => t.Status == request.Status.Value);
        if (request.Priority.HasValue) query = query.Where(t => t.Priority == request.Priority.Value);
        if (request.AssigneeUserId.HasValue) query = query.Where(t => t.AssigneeUserId == request.AssigneeUserId.Value);
        if (request.ReporterUserId.HasValue) query = query.Where(t => t.ReporterUserId == request.ReporterUserId.Value);
        if (request.DepartmentId.HasValue) query = query.Where(t => t.DepartmentId == new DepartmentId(request.DepartmentId.Value));
        if (request.ServiceQueueId.HasValue) query = query.Where(t => t.ServiceQueueId == new ServiceQueueId(request.ServiceQueueId.Value));

        // Kuyruk havuzu filtresi: virgülle ayrılmış queue ID'leri
        if (!string.IsNullOrWhiteSpace(request.QueueIds))
        {
            var queueGuids = request.QueueIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => new ServiceQueueId(Guid.Parse(s.Trim())))
                .ToList();

            // Tek queue varsa basit eşitlik kullan (EF Core sorunu yok)
            if (queueGuids.Count == 1)
            {
                var singleQueue = queueGuids[0];
                query = query.Where(t => t.ServiceQueueId == singleQueue);
            }
            else
            {
                // Birden fazla queue: OR ifadeleri oluştur
                var parameter = Expression.Parameter(typeof(Ticket), "t");
                var sqProp = Expression.Property(parameter, nameof(Ticket.ServiceQueueId));
                Expression? combined = null;
                foreach (var qid in queueGuids)
                {
                    var constVal = Expression.Constant((ServiceQueueId?)qid, typeof(ServiceQueueId?));
                    var eq = Expression.Equal(sqProp, constVal);
                    combined = combined is null ? eq : Expression.OrElse(combined, eq);
                }
                if (combined is not null)
                {
                    var lambda = Expression.Lambda<Func<Ticket, bool>>(combined, parameter);
                    query = query.Where(lambda);
                }
            }
        }

        // Sadece atanmamış ticket'lar
        if (request.UnassignedOnly)
            query = query.Where(t => t.AssigneeUserId == null);

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
            .Include(t => t.ServiceQueue)
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

// ═══════════════════════════════════════════════════════════════
//  Claim Ticket Handler
// ═══════════════════════════════════════════════════════════════

public sealed class ClaimTicketHandler(RequestManagementDbContext db, IEventBus eventBus)
    : IRequestHandler<ClaimTicketCommand>
{
    public async Task Handle(ClaimTicketCommand request, CancellationToken ct)
    {
        var ticket = await db.Tickets.FindAsync([new TicketId(request.TicketId)], ct)
            ?? throw new KeyNotFoundException($"Ticket '{request.TicketId}' not found.");

        if (ticket.AssigneeUserId.HasValue)
            throw new InvalidOperationException("Ticket is already assigned.");

        // Kullanıcının ticket'ın kuyruğunda üye olup olmadığını kontrol et
        if (ticket.ServiceQueueId.HasValue)
        {
            var isMember = await db.QueueMemberships.AnyAsync(
                m => m.QueueId == ticket.ServiceQueueId.Value && m.UserId == request.ClaimerUserId && m.IsActive, ct);
            if (!isMember)
                throw new InvalidOperationException("You are not a member of this ticket's queue.");
        }

        var previousAssignee = ticket.AssigneeUserId;
        ticket.Assign(request.ClaimerUserId);
        await db.SaveChangesAsync(ct);

        await eventBus.PublishAsync(new TicketAssignedEvent(
            ticket.Id.Value, ticket.Number, request.ClaimerUserId, previousAssignee), ct);
    }
}

// ═══════════════════════════════════════════════════════════════
//  GetMyQueues Handler
// ═══════════════════════════════════════════════════════════════

public sealed class GetMyQueuesHandler(RequestManagementDbContext db)
    : IRequestHandler<GetMyQueuesQuery, IReadOnlyList<MyQueueDto>>
{
    public async Task<IReadOnlyList<MyQueueDto>> Handle(GetMyQueuesQuery request, CancellationToken ct)
    {
        // Kullanıcının üye olduğu kuyrukları bul
        var memberships = await db.QueueMemberships
            .Where(m => m.UserId == request.UserId && m.IsActive)
            .Include(m => m.Queue)
                .ThenInclude(q => q.Department)
            .ToListAsync(ct);

        if (memberships.Count == 0) return [];

        var queueIds = memberships.Select(m => m.QueueId).ToHashSet();

        // Her kuyruk için ticket sayıları — raw SQL-friendly yaklaşım
        // Tüm aktif ticket'ları queue bazlı gruplayarak çek
        var allQueueTickets = await db.Tickets
            .Where(t => t.ServiceQueueId.HasValue
                && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Cancelled)
            .GroupBy(t => t.ServiceQueueId!.Value)
            .Select(g => new
            {
                QueueId = g.Key,
                Total = g.Count(),
                Unassigned = g.Count(t => t.AssigneeUserId == null)
            })
            .ToListAsync(ct);

        // Client-side: sadece ilgili kuyrukları filtrele
        var countMap = allQueueTickets
            .Where(c => queueIds.Contains(c.QueueId))
            .ToDictionary(c => c.QueueId);

        return memberships.Select(m =>
        {
            var q = m.Queue;
            countMap.TryGetValue(m.QueueId, out var counts);
            return new MyQueueDto(
                q.Id.Value, q.Name, q.Code, q.Description,
                q.Department?.Name, m.Role,
                counts?.Total ?? 0, counts?.Unassigned ?? 0);
        }).OrderBy(q => q.Name).ToList();
    }
}
