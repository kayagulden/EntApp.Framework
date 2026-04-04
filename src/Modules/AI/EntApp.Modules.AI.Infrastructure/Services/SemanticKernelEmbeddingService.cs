using System.Diagnostics;
using Microsoft.Extensions.AI;
using EntApp.Modules.AI.Application.Interfaces;
using EntApp.Modules.AI.Domain.Entities;
using EntApp.Modules.AI.Domain.Enums;
using EntApp.Modules.AI.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace EntApp.Modules.AI.Infrastructure.Services;

/// <summary>
/// Semantic Kernel üzerinden metin → embedding vektörü.
/// Provider-agnostic: OpenAI text-embedding-3-small, Ollama nomic-embed-text vb.
/// </summary>
public sealed class SemanticKernelEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly AiDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SemanticKernelEmbeddingService> _logger;

    public SemanticKernelEmbeddingService(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        AiDbContext dbContext,
        IConfiguration configuration,
        ILogger<SemanticKernelEmbeddingService> logger)
    {
        _embeddingGenerator = embeddingGenerator;
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var providerName = _configuration.GetValue<string>("AiSettings:DefaultProvider") ?? "OpenAI";
        var modelName = _configuration[$"AiSettings:{providerName}:EmbeddingModel"] ?? "text-embedding-3-small";

        try
        {
            var results = await _embeddingGenerator.GenerateAsync([text], cancellationToken: ct);
            sw.Stop();

            var vector = results[0].Vector.ToArray();

            // Usage log
            var provider = Enum.TryParse<AiProvider>(providerName, out var p) ? p : AiProvider.OpenAI;
            var log = AiUsageLog.CreateSuccess(
                provider, modelName, AiOperation.Embedding,
                inputTokens: EstimateTokens(text), outputTokens: 0,
                estimatedCost: EstimateEmbeddingCost(provider, text),
                durationMs: sw.ElapsedMilliseconds);

            _dbContext.AiUsageLogs.Add(log);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("[AI:Embed] {Provider}/{Model} — {Dims} dims, {Duration}ms",
                providerName, modelName, vector.Length, sw.ElapsedMilliseconds);

            return vector;
        }
        catch (Exception ex)
        {
            sw.Stop();
            var provider = Enum.TryParse<AiProvider>(providerName, out var p) ? p : AiProvider.OpenAI;

            var log = AiUsageLog.CreateFailure(
                provider, modelName, AiOperation.Embedding,
                ex.Message, sw.ElapsedMilliseconds);

            _dbContext.AiUsageLogs.Add(log);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogError(ex, "[AI:Embed] FAILED — {Provider}/{Model}", providerName, modelName);
            throw;
        }
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IEnumerable<string> texts, CancellationToken ct = default)
    {
        var textList = texts.ToList();
        var results = new List<float[]>(textList.Count);

        var embeddings = await _embeddingGenerator.GenerateAsync(textList, cancellationToken: ct);

        foreach (var emb in embeddings)
        {
            results.Add(emb.Vector.ToArray());
        }

        _logger.LogInformation("[AI:EmbedBatch] {Count} texts embedded", textList.Count);
        return results;
    }

    private static int EstimateTokens(string text) => text.Length / 4; // ~4 chars per token

    private static decimal EstimateEmbeddingCost(AiProvider provider, string text) => provider switch
    {
        AiProvider.OpenAI => (EstimateTokens(text) * 0.00002m) / 1000m,
        AiProvider.AzureOpenAI => (EstimateTokens(text) * 0.00002m) / 1000m,
        AiProvider.Ollama => 0m,
        _ => 0m,
    };
}
