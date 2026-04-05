using EntApp.Modules.RequestManagement.Application.Commands;
using EntApp.Modules.RequestManagement.Application.Queries;
using EntApp.Modules.RequestManagement.Domain.Entities;
using EntApp.Modules.RequestManagement.Domain.Ids;
using EntApp.Modules.RequestManagement.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.RequestManagement.Infrastructure.Handlers;

// ═══════════════════════════════════════════════════════════════
//  ServiceQueue Handlers
// ═══════════════════════════════════════════════════════════════

public sealed class CreateServiceQueueHandler(RequestManagementDbContext db)
    : IRequestHandler<CreateServiceQueueCommand, Guid>
{
    public async Task<Guid> Handle(CreateServiceQueueCommand request, CancellationToken ct)
    {
        var queue = ServiceQueue.Create(
            request.Name, request.Code, request.Description,
            request.DepartmentId.HasValue ? new DepartmentId(request.DepartmentId.Value) : null,
            request.ManagerUserId, request.DefaultWorkflowDefinitionId);

        db.ServiceQueues.Add(queue);
        await db.SaveChangesAsync(ct);
        return queue.Id.Value;
    }
}

public sealed class UpdateServiceQueueHandler(RequestManagementDbContext db)
    : IRequestHandler<UpdateServiceQueueCommand>
{
    public async Task Handle(UpdateServiceQueueCommand request, CancellationToken ct)
    {
        var queue = await db.ServiceQueues.FindAsync([new ServiceQueueId(request.Id)], ct)
            ?? throw new KeyNotFoundException($"ServiceQueue '{request.Id}' not found.");

        queue.Update(request.Name, request.Code, request.Description,
            request.DepartmentId.HasValue ? new DepartmentId(request.DepartmentId.Value) : null,
            request.ManagerUserId, request.DefaultWorkflowDefinitionId);

        await db.SaveChangesAsync(ct);
    }
}

// ═══════════════════════════════════════════════════════════════
//  QueueMembership Handlers
// ═══════════════════════════════════════════════════════════════

public sealed class AddQueueMemberHandler(RequestManagementDbContext db)
    : IRequestHandler<AddQueueMemberCommand, Guid>
{
    public async Task<Guid> Handle(AddQueueMemberCommand request, CancellationToken ct)
    {
        // Üyelik kontrolü
        var exists = await db.QueueMemberships.AnyAsync(
            m => m.QueueId == new ServiceQueueId(request.QueueId) && m.UserId == request.UserId, ct);

        if (exists) throw new InvalidOperationException("This user is already a member of this queue.");

        var membership = QueueMembership.Create(
            new ServiceQueueId(request.QueueId), request.UserId, request.Role);

        db.QueueMemberships.Add(membership);
        await db.SaveChangesAsync(ct);
        return membership.Id.Value;
    }
}

public sealed class RemoveQueueMemberHandler(RequestManagementDbContext db)
    : IRequestHandler<RemoveQueueMemberCommand>
{
    public async Task Handle(RemoveQueueMemberCommand request, CancellationToken ct)
    {
        var membership = await db.QueueMemberships.FindAsync([new QueueMembershipId(request.MembershipId)], ct)
            ?? throw new KeyNotFoundException($"Membership '{request.MembershipId}' not found.");

        db.QueueMemberships.Remove(membership);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class UpdateQueueMemberRoleHandler(RequestManagementDbContext db)
    : IRequestHandler<UpdateQueueMemberRoleCommand>
{
    public async Task Handle(UpdateQueueMemberRoleCommand request, CancellationToken ct)
    {
        var membership = await db.QueueMemberships.FindAsync([new QueueMembershipId(request.MembershipId)], ct)
            ?? throw new KeyNotFoundException($"Membership '{request.MembershipId}' not found.");

        membership.UpdateRole(request.Role);
        await db.SaveChangesAsync(ct);
    }
}

// ═══════════════════════════════════════════════════════════════
//  ServiceQueue Query Handlers
// ═══════════════════════════════════════════════════════════════

public sealed class ListServiceQueuesHandler(RequestManagementDbContext db)
    : IRequestHandler<ListServiceQueuesQuery, IReadOnlyList<ServiceQueue>>
{
    public async Task<IReadOnlyList<ServiceQueue>> Handle(ListServiceQueuesQuery request, CancellationToken ct)
    {
        var query = db.ServiceQueues
            .Include(q => q.Department)
            .Include(q => q.Members)
            .AsQueryable();

        if (request.DepartmentId.HasValue)
            query = query.Where(q => q.DepartmentId == new DepartmentId(request.DepartmentId.Value));
        if (request.ActiveOnly) query = query.Where(q => q.IsActive);

        return await query.OrderBy(q => q.Name).ToListAsync(ct);
    }
}

public sealed class GetServiceQueueHandler(RequestManagementDbContext db)
    : IRequestHandler<GetServiceQueueQuery, ServiceQueue?>
{
    public async Task<ServiceQueue?> Handle(GetServiceQueueQuery request, CancellationToken ct)
    {
        return await db.ServiceQueues
            .Include(q => q.Department)
            .Include(q => q.Members)
            .FirstOrDefaultAsync(q => q.Id == new ServiceQueueId(request.Id), ct);
    }
}
