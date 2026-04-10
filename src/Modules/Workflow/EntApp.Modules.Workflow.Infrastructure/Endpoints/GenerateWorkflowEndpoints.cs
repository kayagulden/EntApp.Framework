using EntApp.Modules.Workflow.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Modules.Workflow.Infrastructure.Endpoints;

/// <summary>
/// AI destekli workflow oluşturma ve tarif etme API endpoint'leri.
/// </summary>
public static class GenerateWorkflowEndpoints
{
    public static IEndpointRouteBuilder MapGenerateWorkflowEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workflows/ai")
            .WithTags("Workflow AI");

        // ── Generate: Doğal dil → Workflow ──────────────
        group.MapPost("/generate", async (GenerateWorkflowRequest req, IWorkflowAiService aiService) =>
        {
            if (string.IsNullOrWhiteSpace(req.Prompt))
                return Results.BadRequest(new { error = "Prompt gereklidir. Workflow'u doğal dille tarif edin." });

            try
            {
                var result = await aiService.GenerateFromPromptAsync(req.Prompt, req.Name);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    title: "Workflow oluşturma hatası",
                    statusCode: 500);
            }
        })
        .WithName("GenerateWorkflowWithAi")
        .WithSummary("Doğal dil tarifinden AI ile Elsa workflow oluştur");

        // ── Describe: Mevcut workflow → Doğal dil ───────
        group.MapPost("/describe", async (DescribeWorkflowRequest req, IWorkflowAiService aiService) =>
        {
            if (string.IsNullOrWhiteSpace(req.DefinitionId))
                return Results.BadRequest(new { error = "DefinitionId gereklidir." });

            try
            {
                var result = await aiService.DescribeWorkflowAsync(req.DefinitionId);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    title: "Workflow tarif etme hatası",
                    statusCode: 500);
            }
        })
        .WithName("DescribeWorkflowWithAi")
        .WithSummary("Mevcut bir workflow'u AI ile doğal dilde tarif et");

        return app;
    }
}

// ── Request DTOs ────────────────────────────────────────────
public sealed record GenerateWorkflowRequest(string Prompt, string? Name = null);
public sealed record DescribeWorkflowRequest(string DefinitionId);
