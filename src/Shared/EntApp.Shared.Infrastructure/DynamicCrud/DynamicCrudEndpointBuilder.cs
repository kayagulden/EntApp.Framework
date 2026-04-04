using System.Reflection;
using System.Text.Json;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Infrastructure.DynamicCrud.Export;
using EntApp.Shared.Infrastructure.DynamicCrud.Import;
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

        // ── Import/Export Endpoints ──────────────────────────
        group.MapGet("/{entityName}/export", ExportData)
            .WithName("DynamicCrud_Export")
            .WithDescription("Entity verilerini Excel/CSV olarak indir");

        group.MapGet("/{entityName}/import-template", DownloadTemplate)
            .WithName("DynamicCrud_ImportTemplate")
            .WithDescription("Boş import şablonu indir");

        group.MapPost("/{entityName}/import/preview", ImportPreview)
            .WithName("DynamicCrud_ImportPreview")
            .WithDescription("Import dosyasını parse et ve önizleme döndür")
            .DisableAntiforgery();

        group.MapPost("/{entityName}/import", ImportData)
            .WithName("DynamicCrud_Import")
            .WithDescription("Dosyadan toplu veri aktar")
            .DisableAntiforgery();

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

    // ═══════════════════════════════════════════════════════════
    //  IMPORT / EXPORT HANDLERS
    // ═══════════════════════════════════════════════════════════

    private static async Task<IResult> ExportData(
        string entityName,
        IDynamicExportService exportService,
        string format = "xlsx",
        CancellationToken ct = default)
    {
        try
        {
            byte[] data;
            string contentType;
            string fileName;

            if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                data = await exportService.ExportToCsvAsync(entityName, ct);
                contentType = "text/csv";
                fileName = $"{entityName}.csv";
            }
            else
            {
                data = await exportService.ExportToExcelAsync(entityName, ct);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"{entityName}.xlsx";
            }

            return Results.File(data, contentType, fileName);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { error = $"Entity '{entityName}' not found." });
        }
    }

    private static IResult DownloadTemplate(
        string entityName,
        Export.ExportTemplateBuilder templateBuilder)
    {
        try
        {
            var data = templateBuilder.BuildTemplate(entityName);
            return Results.File(data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{entityName}_template.xlsx");
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { error = $"Entity '{entityName}' not found." });
        }
    }

    private static async Task<IResult> ImportPreview(
        string entityName,
        IFormFile file,
        IDynamicImportService importService,
        CancellationToken ct)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var preview = await importService.ParseFileAsync(
                entityName, stream, file.ContentType, ct);
            return Results.Ok(preview);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { error = $"Entity '{entityName}' not found." });
        }
    }

    private static async Task<IResult> ImportData(
        string entityName,
        IFormFile file,
        IDynamicImportService importService,
        HttpRequest request,
        CancellationToken ct)
    {
        try
        {
            // Kolon eşleştirmesi JSON olarak mapping query param'dan gelir
            var mappingJson = request.Query["mapping"].FirstOrDefault();
            Dictionary<int, string> columnMapping;

            if (!string.IsNullOrEmpty(mappingJson))
            {
                columnMapping = JsonSerializer.Deserialize<Dictionary<int, string>>(mappingJson) ?? new();
            }
            else
            {
                // Mapping yoksa, önce parse edip auto-map yap
                using var previewStream = file.OpenReadStream();
                var preview = await importService.ParseFileAsync(
                    entityName, previewStream, file.ContentType, ct);
                columnMapping = preview.SuggestedMapping;
            }

            using var stream = file.OpenReadStream();
            var result = await importService.ImportAsync(
                entityName, stream, file.ContentType, columnMapping, ct);
            return Results.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { error = $"Entity '{entityName}' not found." });
        }
    }
}
