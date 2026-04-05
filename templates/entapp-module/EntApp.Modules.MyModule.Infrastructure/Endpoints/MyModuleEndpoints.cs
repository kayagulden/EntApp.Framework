using EntApp.Modules.MyModule.Domain.Entities;
using EntApp.Modules.MyModule.Domain.Enums;
using EntApp.Modules.MyModule.Domain.Ids;
using EntApp.Modules.MyModule.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.MyModule.Infrastructure.Endpoints;

/// <summary>MyModule REST API endpoint'leri.</summary>
public static class MyModuleEndpoints
{
    public static IEndpointRouteBuilder MapMyModuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/moduleschema/sample-entities")
            .WithTags("MyModule - SampleEntities");

        // ── List ──────────────────────────────────────────────────
        group.MapGet("/", async (MyModuleDbContext db,
            string? search, int page = 1, int pageSize = 20) =>
        {
            var query = db.SampleEntities.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(e => e.Name.Contains(search));

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new { items, totalCount = total, page, pageSize });
        });

        // ── Get by ID ────────────────────────────────────────────
        group.MapGet("/{id:guid}", async (Guid id, MyModuleDbContext db) =>
        {
            var entity = await db.SampleEntities
                .FirstOrDefaultAsync(e => e.Id == new SampleEntityId(id));

            return entity is null ? Results.NotFound() : Results.Ok(entity);
        });

        // ── Create ───────────────────────────────────────────────
        group.MapPost("/", async (CreateSampleRequest req, MyModuleDbContext db) =>
        {
            var entity = SampleEntity.Create(req.Name, req.Description);
            db.SampleEntities.Add(entity);
            await db.SaveChangesAsync();

            return Results.Created($"/api/moduleschema/sample-entities/{entity.Id.Value}", entity);
        });

        // ── Update ───────────────────────────────────────────────
        group.MapPut("/{id:guid}", async (Guid id, UpdateSampleRequest req, MyModuleDbContext db) =>
        {
            var entity = await db.SampleEntities
                .FirstOrDefaultAsync(e => e.Id == new SampleEntityId(id));

            if (entity is null) return Results.NotFound();

            entity.Name = req.Name;
            entity.Description = req.Description;
            entity.Status = req.Status;
            await db.SaveChangesAsync();

            return Results.Ok(entity);
        });

        // ── Delete ───────────────────────────────────────────────
        group.MapDelete("/{id:guid}", async (Guid id, MyModuleDbContext db) =>
        {
            var entity = await db.SampleEntities
                .FirstOrDefaultAsync(e => e.Id == new SampleEntityId(id));

            if (entity is null) return Results.NotFound();

            db.SampleEntities.Remove(entity);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return app;
    }

    // ── Request DTO'lar ──────────────────────────────────────────
    public record CreateSampleRequest(string Name, string? Description);
    public record UpdateSampleRequest(string Name, string? Description, SampleStatus Status);
}
