using System.Reflection;
using System.Text.Json;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Infrastructure.DynamicCrud.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Shared.Infrastructure.DynamicCrud;

/// <summary>
/// [DynamicEntity] attribute'ü olan her entity için
/// minimal API endpoint'lerini otomatik register eder.
/// </summary>
public static class DynamicCrudEndpointBuilder
{
    /// <summary>
    /// Dynamic CRUD endpoint'lerini register eder.
    /// Program.cs'de: app.MapDynamicCrudEndpoints();
    /// </summary>
    public static IEndpointRouteBuilder MapDynamicCrudEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dynamic")
            .WithTags("DynamicCrud");

        // ── Metadata Endpoints ──────────────────────────────
        group.MapGet("/meta/menu", GetMenu)
            .WithName("DynamicCrud_GetMenu")
            .WithDescription("Sidebar menu yapısını döner");

        group.MapGet("/meta/{entityName}", GetMetadata)
            .WithName("DynamicCrud_GetMetadata")
            .WithDescription("Entity metadata schema döner");

        // ── CRUD Endpoints ──────────────────────────────────
        group.MapGet("/{entityName}", GetPaged)
            .WithName("DynamicCrud_GetPaged")
            .WithDescription("Sayfalanmış entity listesi");

        group.MapGet("/{entityName}/{id:guid}", GetById)
            .WithName("DynamicCrud_GetById")
            .WithDescription("Tekil entity kaydı");

        group.MapPost("/{entityName}", Create)
            .WithName("DynamicCrud_Create")
            .WithDescription("Yeni entity kaydı oluştur");

        group.MapPut("/{entityName}/{id:guid}", Update)
            .WithName("DynamicCrud_Update")
            .WithDescription("Entity kaydı güncelle");

        group.MapDelete("/{entityName}/{id:guid}", Delete)
            .WithName("DynamicCrud_Delete")
            .WithDescription("Entity kaydı sil (soft delete)");

        group.MapGet("/{entityName}/lookup", Lookup)
            .WithName("DynamicCrud_Lookup")
            .WithDescription("Lookup arama (dropdown/combobox)");

        return app;
    }

    // ═══════════════════════════════════════════════════════════
    //  ENDPOINT HANDLERS
    // ═══════════════════════════════════════════════════════════

    private static IResult GetMenu(IMetadataService metadataService)
    {
        var menu = metadataService.GetMenu();
        return Results.Ok(menu);
    }

    private static IResult GetMetadata(string entityName, IMetadataService metadataService)
    {
        var metadata = metadataService.GetMetadata(entityName);
        if (metadata is null)
            return Results.NotFound(new { error = $"Entity '{entityName}' not found." });

        return Results.Ok(metadata);
    }

    private static async Task<IResult> GetPaged(
        string entityName,
        IDynamicCrudService crudService,
        int pageNumber = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool sortDescending = false,
        string? search = null,
        CancellationToken ct = default)
    {
        try
        {
            var request = new PagedRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDescending = sortDescending,
                SearchTerm = search
            };

            var result = await crudService.GetPagedAsync(entityName, request, ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { error = $"Entity '{entityName}' not found." });
        }
    }

    private static async Task<IResult> GetById(
        string entityName,
        Guid id,
        IDynamicCrudService crudService,
        CancellationToken ct)
    {
        try
        {
            var result = await crudService.GetByIdAsync(entityName, id, ct);
            if (result is null)
                return Results.NotFound(new { error = $"'{entityName}' with id '{id}' not found." });

            return Results.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { error = $"Entity '{entityName}' not found." });
        }
    }

    private static async Task<IResult> Create(
        string entityName,
        JsonElement body,
        IDynamicCrudService crudService,
        CancellationToken ct)
    {
        try
        {
            var id = await crudService.CreateAsync(entityName, body, ct);
            return Results.Created($"/api/v1/dynamic/{entityName}/{id}", new { id });
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { error = $"Entity '{entityName}' not found." });
        }
    }

    private static async Task<IResult> Update(
        string entityName,
        Guid id,
        JsonElement body,
        IDynamicCrudService crudService,
        CancellationToken ct)
    {
        try
        {
            await crudService.UpdateAsync(entityName, id, body, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Delete(
        string entityName,
        Guid id,
        IDynamicCrudService crudService,
        CancellationToken ct)
    {
        try
        {
            await crudService.DeleteAsync(entityName, id, ct);
            return Results.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Lookup(
        string entityName,
        IDynamicCrudService crudService,
        string? search = null,
        int take = 20,
        CancellationToken ct = default)
    {
        try
        {
            var result = await crudService.LookupAsync(entityName, search, take, ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { error = $"Entity '{entityName}' not found." });
        }
    }
}
