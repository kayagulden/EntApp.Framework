using EntApp.Modules.AI.Application.Interfaces;
using EntApp.Modules.AI.Domain.Entities;
using EntApp.Modules.AI.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Pgvector;

namespace EntApp.Modules.AI.Infrastructure.Endpoints;

/// <summary>
/// AI module REST API endpoints.
/// POST /api/ai/embed   — metin → embedding vektörü
/// POST /api/ai/store   — chunk + embedding → DB
/// POST /api/ai/search  — query → benzer dokümanlar
/// </summary>
public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai")
            .WithTags("AI");

        // ── Embed ────────────────────────────────────────
        group.MapPost("/embed", async (EmbedRequest req, HttpContext ctx) =>
        {
            if (string.IsNullOrWhiteSpace(req.Text))
                return Results.BadRequest(new { error = "Text is required." });

            var embeddingService = ctx.RequestServices.GetService<IEmbeddingService>();
            if (embeddingService is null)
                return Results.Problem("Embedding service not configured. Please set an API key.", statusCode: 501);

            var vector = await embeddingService.EmbedAsync(req.Text);
            return Results.Ok(new EmbedResponse(vector, vector.Length));
        })
        .WithName("EmbedText")
        .WithSummary("Metin → embedding vektörü üretir");

        // ── Store ────────────────────────────────────────
        group.MapPost("/store", async (StoreRequest req, HttpContext ctx, PgVectorStore store) =>
        {
            if (string.IsNullOrWhiteSpace(req.Content))
                return Results.BadRequest(new { error = "Content is required." });

            var embeddingService = ctx.RequestServices.GetService<IEmbeddingService>();
            if (embeddingService is null)
                return Results.Problem("Embedding service not configured. Please set an API key.", statusCode: 501);

            var vector = await embeddingService.EmbedAsync(req.Content);

            var doc = EmbeddingDocument.Create(
                moduleName: req.ModuleName ?? "General",
                sourceType: req.SourceType ?? "Text",
                content: req.Content,
                chunkIndex: req.ChunkIndex,
                tokenCount: req.Content.Length / 4,
                embedding: new Vector(vector),
                sourceId: req.SourceId,
                metadata: req.Metadata);

            await store.StoreAsync(doc);

            return Results.Created($"/api/ai/store/{doc.Id}", new { id = doc.Id, dimensions = vector.Length });
        })
        .WithName("StoreEmbedding")
        .WithSummary("Metin chunk → embed + pgvector'e kaydet");

        // ── Search ───────────────────────────────────────
        group.MapPost("/search", async (SearchRequest req, HttpContext ctx, PgVectorStore store) =>
        {
            if (string.IsNullOrWhiteSpace(req.Query))
                return Results.BadRequest(new { error = "Query is required." });

            var embeddingService = ctx.RequestServices.GetService<IEmbeddingService>();
            if (embeddingService is null)
                return Results.Problem("Embedding service not configured. Please set an API key.", statusCode: 501);

            var queryVector = await embeddingService.EmbedAsync(req.Query);

            var results = await store.SearchAsync(
                new Vector(queryVector),
                topK: req.TopK,
                minScore: req.MinScore,
                moduleName: req.ModuleName);

            var response = results.Select(r => new SearchResultItem(
                r.Document.Id,
                r.Document.Content,
                r.Document.ModuleName,
                r.Document.SourceType,
                r.Document.SourceId,
                r.Score,
                r.Document.ChunkIndex
            )).ToList();

            return Results.Ok(new SearchResponse(response, req.Query));
        })
        .WithName("SearchSimilar")
        .WithSummary("Semantic search — query → benzer dokümanlar");

        // ── RAG ──────────────────────────────────────────
        group.MapPost("/rag", async (Application.DTOs.RagRequest req, HttpContext ctx) =>
        {
            if (string.IsNullOrWhiteSpace(req.Query))
                return Results.BadRequest(new { error = "Query is required." });

            var ragService = ctx.RequestServices.GetService<IRagService>();
            if (ragService is null)
                return Results.Problem("RAG service not configured. Please set an API key.", statusCode: 501);

            var response = await ragService.QueryAsync(req);
            return Results.Ok(response);
        })
        .WithName("RagQuery")
        .WithSummary("RAG — soru sor, bağlam dokümanlarıyla zenginleştirilmiş yanıt al");

        // ── Ingest ───────────────────────────────────────
        group.MapPost("/ingest", async (HttpRequest httpReq, HttpContext ctx, PgVectorStore store) =>
        {
            var form = await httpReq.ReadFormAsync();
            var file = form.Files.FirstOrDefault();
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "File is required." });

            var embeddingService = ctx.RequestServices.GetService<IEmbeddingService>();
            if (embeddingService is null)
                return Results.Problem("Embedding service not configured. Please set an API key.", statusCode: 501);

            var docProcessor = ctx.RequestServices.GetRequiredService<IDocumentProcessor>();

            var moduleName = form["moduleName"].FirstOrDefault() ?? "General";
            var sourceType = form["sourceType"].FirstOrDefault() ?? Path.GetExtension(file.FileName)?.TrimStart('.') ?? "File";
            var sourceId = form["sourceId"].FirstOrDefault();

            // Parse & chunk
            using var stream = file.OpenReadStream();
            var chunks = await docProcessor.ProcessAsync(stream, file.FileName);

            if (chunks.Count == 0)
                return Results.Ok(new IngestResponse(0, file.FileName, "No text extracted."));

            // Embed & store
            var texts = chunks.Select(c => c.Content).ToList();
            var embeddings = await embeddingService.EmbedBatchAsync(texts);

            var docs = chunks.Select((chunk, i) => EmbeddingDocument.Create(
                moduleName: moduleName,
                sourceType: sourceType,
                content: chunk.Content,
                chunkIndex: chunk.Index,
                tokenCount: chunk.TokenCount,
                embedding: new Vector(embeddings[i]),
                sourceId: sourceId,
                metadata: $"{{\"fileName\":\"{file.FileName}\"}}")
            ).ToList();

            await store.StoreBatchAsync(docs);

            return Results.Ok(new IngestResponse(docs.Count, file.FileName, "Ingested successfully."));
        })
        .WithName("IngestDocument")
        .WithSummary("Dosya yükle → parse → chunk → embed → pgvector'e kaydet")
        .DisableAntiforgery();

        return app;
    }
}

// ── Request / Response DTOs ──────────────────────────────

public sealed record EmbedRequest(string Text);
public sealed record EmbedResponse(float[] Vector, int Dimensions);

public sealed record StoreRequest(
    string Content,
    string? ModuleName = null,
    string? SourceType = null,
    string? SourceId = null,
    string? Metadata = null,
    int ChunkIndex = 0);

public sealed record SearchRequest(
    string Query,
    int TopK = 5,
    double MinScore = 0.7,
    string? ModuleName = null);

public sealed record SearchResultItem(
    Guid Id,
    string Content,
    string ModuleName,
    string SourceType,
    string? SourceId,
    double Score,
    int ChunkIndex);

public sealed record SearchResponse(
    IReadOnlyList<SearchResultItem> Results,
    string Query);

public sealed record IngestResponse(
    int ChunkCount,
    string FileName,
    string Message);
