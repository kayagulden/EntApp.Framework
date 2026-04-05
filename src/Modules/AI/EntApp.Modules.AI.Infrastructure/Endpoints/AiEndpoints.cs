using EntApp.Modules.AI.Application.Commands;
using EntApp.Modules.AI.Application.DTOs;
using EntApp.Modules.AI.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EntApp.Modules.AI.Infrastructure.Endpoints;

/// <summary>AI module REST API endpoints — CQRS/MediatR ile.</summary>
public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai").WithTags("AI");

        group.MapPost("/embed", async (EmbedRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new EmbedTextCommand(req.Text));
            return Results.Ok(result);
        }).WithName("EmbedText").WithSummary("Metin → embedding vektörü üretir");

        group.MapPost("/store", async (StoreRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new StoreEmbeddingCommand(req.Content,
                req.ModuleName, req.SourceType, req.SourceId, req.Metadata, req.ChunkIndex));
            return Results.Created($"/api/ai/store/{result.Id}", result);
        }).WithName("StoreEmbedding").WithSummary("Metin chunk → embed + pgvector'e kaydet");

        group.MapPost("/search", async (SearchRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new SearchSimilarQuery(req.Query, req.TopK, req.MinScore, req.ModuleName));
            return Results.Ok(result);
        }).WithName("SearchSimilar").WithSummary("Semantic search — query → benzer dokümanlar");

        group.MapPost("/rag", async (RagQueryRequest req, ISender mediator) =>
        {
            var result = await mediator.Send(new RagQuery(req.Query, req.TopK, req.MinScore, req.ModuleName, req.SystemPrompt));
            return Results.Ok(result);
        }).WithName("RagQuery").WithSummary("RAG — soru sor, bağlam dokümanlarıyla zenginleştirilmiş yanıt al");

        group.MapPost("/ingest", async (HttpRequest httpReq, ISender mediator) =>
        {
            var form = await httpReq.ReadFormAsync();
            var file = form.Files.FirstOrDefault();
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "File is required." });

            using var stream = file.OpenReadStream();
            var moduleName = form["moduleName"].FirstOrDefault() ?? "General";
            var sourceType = form["sourceType"].FirstOrDefault() ?? Path.GetExtension(file.FileName)?.TrimStart('.') ?? "File";
            var sourceId = form["sourceId"].FirstOrDefault();

            var result = await mediator.Send(new IngestDocumentCommand(stream, file.FileName, moduleName, sourceType, sourceId));
            return Results.Ok(result);
        }).WithName("IngestDocument").WithSummary("Dosya yükle → parse → chunk → embed → pgvector'e kaydet").DisableAntiforgery();

        return app;
    }
}

// ── Request DTOs (endpoint-specific) ─────────────────────
public sealed record EmbedRequest(string Text);
public sealed record StoreRequest(string Content, string? ModuleName = null,
    string? SourceType = null, string? SourceId = null, string? Metadata = null, int ChunkIndex = 0);
public sealed record SearchRequest(string Query, int TopK = 5, double MinScore = 0.7, string? ModuleName = null);
public sealed record RagQueryRequest(string Query, int TopK = 5, double MinScore = 0.7,
    string? ModuleName = null, string? SystemPrompt = null);
