using EntApp.Modules.AI.Domain.Entities;
using EntApp.Modules.AI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace EntApp.Modules.AI.Infrastructure.Services;

/// <summary>
/// PostgreSQL pgvector tabanlı vektör deposu.
/// EmbeddingDocument CRUD + cosine similarity search.
/// </summary>
public sealed class PgVectorStore
{
    private readonly AiDbContext _dbContext;
    private readonly ILogger<PgVectorStore> _logger;

    public PgVectorStore(AiDbContext dbContext, ILogger<PgVectorStore> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>Tek doküman chunk kaydet.</summary>
    public async Task StoreAsync(EmbeddingDocument doc, CancellationToken ct = default)
    {
        _dbContext.EmbeddingDocuments.Add(doc);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogDebug("[VectorStore] Stored chunk {ChunkIndex} for {Module}/{SourceType}",
            doc.ChunkIndex, doc.ModuleName, doc.SourceType);
    }

    /// <summary>Toplu doküman chunk kaydet.</summary>
    public async Task StoreBatchAsync(IEnumerable<EmbeddingDocument> docs, CancellationToken ct = default)
    {
        var list = docs.ToList();
        _dbContext.EmbeddingDocuments.AddRange(list);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("[VectorStore] Stored {Count} chunks", list.Count);
    }

    /// <summary>
    /// Cosine similarity ile en benzer doküman parçalarını bul.
    /// </summary>
    public async Task<IReadOnlyList<SimilarityResult>> SearchAsync(
        Vector queryVector,
        int topK = 5,
        double minScore = 0.7,
        string? moduleName = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.EmbeddingDocuments
            .Where(d => d.Embedding != null);

        if (!string.IsNullOrEmpty(moduleName))
        {
            query = query.Where(d => d.ModuleName == moduleName);
        }

        var results = await query
            .Select(d => new
            {
                Document = d,
                Distance = d.Embedding!.CosineDistance(queryVector)
            })
            .OrderBy(x => x.Distance)
            .Take(topK)
            .ToListAsync(ct);

        // Distance → Score dönüşümü (cosine: distance=0 → score=1)
        var scored = results
            .Select(r => new SimilarityResult
            {
                Document = r.Document,
                Score = 1.0 - r.Distance
            })
            .Where(r => r.Score >= minScore)
            .ToList();

        _logger.LogDebug("[VectorStore] Search: {TopK} requested, {Found} found (min={MinScore})",
            topK, scored.Count, minScore);

        return scored;
    }

    /// <summary>Kaynak bazlı silme (modül + sourceType + sourceId).</summary>
    public async Task<int> DeleteBySourceAsync(
        string moduleName, string sourceType, string? sourceId = null, CancellationToken ct = default)
    {
        var query = _dbContext.EmbeddingDocuments
            .Where(d => d.ModuleName == moduleName && d.SourceType == sourceType);

        if (!string.IsNullOrEmpty(sourceId))
        {
            query = query.Where(d => d.SourceId == sourceId);
        }

        var docs = await query.ToListAsync(ct);
        _dbContext.EmbeddingDocuments.RemoveRange(docs);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("[VectorStore] Deleted {Count} chunks for {Module}/{SourceType}/{SourceId}",
            docs.Count, moduleName, sourceType, sourceId ?? "*");

        return docs.Count;
    }
}

/// <summary>Benzerlik arama sonucu.</summary>
public sealed class SimilarityResult
{
    public EmbeddingDocument Document { get; init; } = null!;
    public double Score { get; init; }
}
