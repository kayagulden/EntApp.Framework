using EntApp.Modules.AI.Application.Interfaces;
using EntApp.Modules.AI.Domain.Entities;
using EntApp.Modules.AI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.AI.Infrastructure.Endpoints;

/// <summary>
/// Prompt Template yönetim API endpoint'leri.
/// CRUD + versiyonlama + render/test.
/// </summary>
public static class PromptEndpoints
{
    public static IEndpointRouteBuilder MapPromptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai/prompts")
            .WithTags("Prompts");

        // ── List ─────────────────────────────────────────
        group.MapGet("/", async (AiDbContext db, string? category, int page = 1, int pageSize = 20) =>
        {
            var query = db.PromptTemplates.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(t => t.Category == category);

            // Her key için en yüksek versiyon
            var templates = await query
                .GroupBy(t => t.Key)
                .Select(g => g.OrderByDescending(t => t.Version).First())
                .OrderBy(t => t.Category).ThenBy(t => t.Key)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var total = await query.Select(t => t.Key).Distinct().CountAsync();

            return Results.Ok(new
            {
                items = templates.Select(ToDto),
                totalCount = total,
                pageNumber = page,
                pageSize
            });
        })
        .WithName("ListPrompts")
        .WithSummary("Prompt şablonlarını listele (her key için son versiyon)");

        // ── Get by Key ───────────────────────────────────
        group.MapGet("/{key}", async (string key, AiDbContext db, int? version) =>
        {
            var query = db.PromptTemplates.Where(t => t.Key == key && t.IsActive);

            PromptTemplate? template;
            if (version.HasValue)
            {
                template = await query.FirstOrDefaultAsync(t => t.Version == version.Value);
            }
            else
            {
                template = await query.OrderByDescending(t => t.Version).FirstOrDefaultAsync();
            }

            return template is null
                ? Results.NotFound(new { error = $"Prompt '{key}' not found." })
                : Results.Ok(ToDto(template));
        })
        .WithName("GetPrompt")
        .WithSummary("Key (ve opsiyonel versiyon) ile prompt al");

        // ── Get versions ─────────────────────────────────
        group.MapGet("/{key}/versions", async (string key, AiDbContext db) =>
        {
            var versions = await db.PromptTemplates
                .Where(t => t.Key == key)
                .OrderByDescending(t => t.Version)
                .Select(t => new { t.Version, t.Title, t.IsActive, t.CreatedAt, t.UpdatedAt })
                .ToListAsync();

            return versions.Count == 0
                ? Results.NotFound(new { error = $"Prompt '{key}' not found." })
                : Results.Ok(new { key, versions });
        })
        .WithName("GetPromptVersions")
        .WithSummary("Bir prompt key'inin tüm versiyonlarını listele");

        // ── Create ───────────────────────────────────────
        group.MapPost("/", async (CreatePromptRequest req, AiDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Key) || string.IsNullOrWhiteSpace(req.TemplateContent))
                return Results.BadRequest(new { error = "Key and TemplateContent are required." });

            // Mevcut en yüksek versiyonu bul
            var maxVersion = await db.PromptTemplates
                .Where(t => t.Key == req.Key)
                .MaxAsync(t => (int?)t.Version) ?? 0;

            var template = PromptTemplate.Create(
                key: req.Key,
                title: req.Title ?? req.Key,
                templateContent: req.TemplateContent,
                category: req.Category,
                version: maxVersion + 1);

            db.PromptTemplates.Add(template);
            await db.SaveChangesAsync();

            return Results.Created($"/api/ai/prompts/{template.Key}?version={template.Version}",
                ToDto(template));
        })
        .WithName("CreatePrompt")
        .WithSummary("Yeni prompt şablonu oluştur (otomatik versiyon)");

        // ── Render / Test ────────────────────────────────
        group.MapPost("/render", async (RenderPromptRequest req, IPromptManager promptManager) =>
        {
            if (string.IsNullOrWhiteSpace(req.Key))
                return Results.BadRequest(new { error = "Key is required." });

            try
            {
                var rendered = await promptManager.RenderAsync(req.Key, req.Variables ?? new { });
                return Results.Ok(new { key = req.Key, rendered });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new { error = $"Prompt '{req.Key}' not found." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("RenderPrompt")
        .WithSummary("Prompt şablonunu test et — değişkenleri doldur ve sonucu gör");

        // ── Render inline ────────────────────────────────
        group.MapPost("/render-inline", (RenderInlineRequest req) =>
        {
            if (string.IsNullOrWhiteSpace(req.Template))
                return Results.BadRequest(new { error = "Template is required." });

            try
            {
                var scribanTemplate = Scriban.Template.Parse(req.Template);
                if (scribanTemplate.HasErrors)
                {
                    var errors = string.Join("; ", scribanTemplate.Messages.Select(m => m.Message));
                    return Results.BadRequest(new { error = $"Parse error: {errors}" });
                }

                var rendered = scribanTemplate.Render(req.Variables ?? new { });
                return Results.Ok(new { rendered });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("RenderInlinePrompt")
        .WithSummary("Inline şablon render — DB kullanmadan Scriban test");

        return app;
    }

    private static object ToDto(PromptTemplate t) => new
    {
        t.Id,
        t.Key,
        t.Version,
        t.Title,
        t.TemplateContent,
        t.Category,
        t.IsActive,
        t.CreatedAt,
        t.UpdatedAt
    };
}

// ── DTOs ─────────────────────────────────────────────────

public sealed record CreatePromptRequest(
    string Key,
    string TemplateContent,
    string? Title = null,
    string? Category = null);

public sealed record RenderPromptRequest(
    string Key,
    object? Variables = null);

public sealed record RenderInlineRequest(
    string Template,
    object? Variables = null);
