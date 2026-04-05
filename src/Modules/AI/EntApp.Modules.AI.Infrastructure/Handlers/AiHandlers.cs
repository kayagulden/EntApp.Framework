using EntApp.Modules.AI.Application.Commands;
using EntApp.Modules.AI.Application.DTOs;
using EntApp.Modules.AI.Application.Interfaces;
using EntApp.Modules.AI.Application.Queries;
using EntApp.Modules.AI.Domain.Entities;
using EntApp.Modules.AI.Infrastructure.Services;
using MediatR;
using Pgvector;

namespace EntApp.Modules.AI.Infrastructure.Handlers;

// ── Command Handlers ────────────────────────────────────────
public sealed class EmbedTextCommandHandler(IEmbeddingService embeddingService)
    : IRequestHandler<EmbedTextCommand, EmbedResponse>
{
    public async Task<EmbedResponse> Handle(EmbedTextCommand request, CancellationToken ct)
    {
        var vector = await embeddingService.EmbedAsync(request.Text);
        return new EmbedResponse(vector, vector.Length);
    }
}

public sealed class StoreEmbeddingCommandHandler(IEmbeddingService embeddingService, PgVectorStore store)
    : IRequestHandler<StoreEmbeddingCommand, StoreEmbeddingResult>
{
    public async Task<StoreEmbeddingResult> Handle(StoreEmbeddingCommand request, CancellationToken ct)
    {
        var vector = await embeddingService.EmbedAsync(request.Content);
        var doc = EmbeddingDocument.Create(
            moduleName: request.ModuleName ?? "General",
            sourceType: request.SourceType ?? "Text",
            content: request.Content,
            chunkIndex: request.ChunkIndex,
            tokenCount: request.Content.Length / 4,
            embedding: new Vector(vector),
            sourceId: request.SourceId,
            metadata: request.Metadata);
        await store.StoreAsync(doc);
        return new StoreEmbeddingResult(doc.Id, vector.Length);
    }
}

public sealed class IngestDocumentCommandHandler(IEmbeddingService embeddingService, PgVectorStore store, IDocumentProcessor docProcessor)
    : IRequestHandler<IngestDocumentCommand, IngestResponse>
{
    public async Task<IngestResponse> Handle(IngestDocumentCommand request, CancellationToken ct)
    {
        var chunks = await docProcessor.ProcessAsync(request.FileStream, request.FileName);
        if (chunks.Count == 0)
            return new IngestResponse(0, request.FileName, "No text extracted.");

        var texts = chunks.Select(c => c.Content).ToList();
        var embeddings = await embeddingService.EmbedBatchAsync(texts);

        var docs = chunks.Select((chunk, i) => EmbeddingDocument.Create(
            moduleName: request.ModuleName ?? "General",
            sourceType: request.SourceType ?? Path.GetExtension(request.FileName)?.TrimStart('.') ?? "File",
            content: chunk.Content,
            chunkIndex: chunk.Index,
            tokenCount: chunk.TokenCount,
            embedding: new Vector(embeddings[i]),
            sourceId: request.SourceId,
            metadata: $"{{\"fileName\":\"{request.FileName}\"}}")).ToList();

        await store.StoreBatchAsync(docs);
        return new IngestResponse(docs.Count, request.FileName, "Ingested successfully.");
    }
}

// ── Query Handlers ──────────────────────────────────────────
public sealed class SearchSimilarQueryHandler(IEmbeddingService embeddingService, PgVectorStore store)
    : IRequestHandler<SearchSimilarQuery, SearchResponse>
{
    public async Task<SearchResponse> Handle(SearchSimilarQuery request, CancellationToken ct)
    {
        var queryVector = await embeddingService.EmbedAsync(request.Query);
        var results = await store.SearchAsync(new Vector(queryVector),
            topK: request.TopK, minScore: request.MinScore, moduleName: request.ModuleName);

        var items = results.Select(r => new SearchResultItem(
            r.Document.Id, r.Document.Content, r.Document.ModuleName,
            r.Document.SourceType, r.Document.SourceId, r.Score, r.Document.ChunkIndex)).ToList();

        return new SearchResponse(items, request.Query);
    }
}

public sealed class RagQueryHandler(IRagService ragService) : IRequestHandler<RagQuery, object>
{
    public async Task<object> Handle(RagQuery request, CancellationToken ct)
    {
        var ragReq = new RagRequest
        {
            Query = request.Query,
            TopK = request.TopK,
            MinScore = (float)request.MinScore,
            ModuleName = request.ModuleName,
            SystemPrompt = request.SystemPrompt
        };
        return await ragService.QueryAsync(ragReq);
    }
}
