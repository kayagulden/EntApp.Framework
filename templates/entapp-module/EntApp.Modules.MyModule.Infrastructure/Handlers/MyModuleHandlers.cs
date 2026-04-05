using EntApp.Modules.MyModule.Application.Commands;
using EntApp.Modules.MyModule.Application.Queries;
using EntApp.Modules.MyModule.Domain.Entities;
using EntApp.Modules.MyModule.Domain.Ids;
using EntApp.Modules.MyModule.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.MyModule.Infrastructure.Handlers;

/// <summary>
/// CQRS handler'ları — tüm iş mantığı burada yaşar.
/// Endpoint'ler sadece thin proxy olarak ISender.Send() çağırır.
/// </summary>

// ── Query Handlers ──────────────────────────────────────────
public sealed class ListSampleEntitiesQueryHandler(MyModuleDbContext db)
    : IRequestHandler<ListSampleEntitiesQuery, PagedResult<SampleEntityListItem>>
{
    public async Task<PagedResult<SampleEntityListItem>> Handle(
        ListSampleEntitiesQuery request, CancellationToken ct)
    {
        var query = db.SampleEntities.AsQueryable();

        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(e => e.Name.Contains(request.Search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(e => new SampleEntityListItem(
                e.Id.Value, e.Name, e.Description,
                e.Status.ToString(), e.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<SampleEntityListItem>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.Page,
            PageSize = request.PageSize
        };
    }
}

public sealed class GetSampleEntityQueryHandler(MyModuleDbContext db)
    : IRequestHandler<GetSampleEntityQuery, object?>
{
    public async Task<object?> Handle(GetSampleEntityQuery request, CancellationToken ct)
    {
        return await db.SampleEntities
            .FirstOrDefaultAsync(e => e.Id == new SampleEntityId(request.Id), ct);
    }
}

// ── Command Handlers ────────────────────────────────────────
public sealed class CreateSampleEntityCommandHandler(MyModuleDbContext db)
    : IRequestHandler<CreateSampleEntityCommand, CreateSampleEntityResult>
{
    public async Task<CreateSampleEntityResult> Handle(
        CreateSampleEntityCommand request, CancellationToken ct)
    {
        var entity = SampleEntity.Create(request.Name, request.Description);
        db.SampleEntities.Add(entity);
        await db.SaveChangesAsync(ct);
        return new CreateSampleEntityResult(entity.Id.Value, entity.Name);
    }
}

public sealed class UpdateSampleEntityCommandHandler(MyModuleDbContext db)
    : IRequestHandler<UpdateSampleEntityCommand, bool>
{
    public async Task<bool> Handle(UpdateSampleEntityCommand request, CancellationToken ct)
    {
        var entity = await db.SampleEntities
            .FirstOrDefaultAsync(e => e.Id == new SampleEntityId(request.Id), ct)
            ?? throw new KeyNotFoundException($"SampleEntity {request.Id} not found.");

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Status = request.Status;
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public sealed class DeleteSampleEntityCommandHandler(MyModuleDbContext db)
    : IRequestHandler<DeleteSampleEntityCommand, bool>
{
    public async Task<bool> Handle(DeleteSampleEntityCommand request, CancellationToken ct)
    {
        var entity = await db.SampleEntities
            .FirstOrDefaultAsync(e => e.Id == new SampleEntityId(request.Id), ct)
            ?? throw new KeyNotFoundException($"SampleEntity {request.Id} not found.");

        db.SampleEntities.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
