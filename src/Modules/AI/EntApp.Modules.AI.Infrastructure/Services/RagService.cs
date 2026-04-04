using EntApp.Modules.AI.Application.DTOs;
using EntApp.Modules.AI.Application.Interfaces;
using EntApp.Modules.AI.Domain.Entities;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace EntApp.Modules.AI.Infrastructure.Services;

/// <summary>
/// RAG (Retrieval-Augmented Generation) servisi.
/// Pipeline: query → embed → vector search → context oluştur → LLM → yanıt.
/// </summary>
public sealed class RagService : IRagService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly ILlmService _llmService;
    private readonly PgVectorStore _vectorStore;
    private readonly ILogger<RagService> _logger;

    private const string RagSystemPrompt =
        """
        Sen bir bilgi asistanısın. Aşağıdaki bağlam dokümanlarını kullanarak kullanıcının sorusunu yanıtla.

        Kurallar:
        - SADECE verilen bağlamdan bilgi kullan
        - Bağlamda bilgi yoksa "Bu konuda yeterli bilgiye sahip değilim" de
        - Yanıtını net ve anlaşılır tut
        - Kaynağa atıfta bulun (ör: "Belge X'e göre...")

        BAĞLAM:
        {{context}}
        """;

    public RagService(
        IEmbeddingService embeddingService,
        ILlmService llmService,
        PgVectorStore vectorStore,
        ILogger<RagService> logger)
    {
        _embeddingService = embeddingService;
        _llmService = llmService;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<RagResponse> QueryAsync(RagRequest request, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        _logger.LogInformation("[RAG] Query: {Query}, TopK: {TopK}, Module: {Module}",
            request.Query, request.TopK, request.ModuleName ?? "*");

        // 1. Query → embedding
        var queryVector = await _embeddingService.EmbedAsync(request.Query, ct);

        // 2. Vector search — benzer chunk'ları bul
        var searchResults = await _vectorStore.SearchAsync(
            new Vector(queryVector),
            topK: request.TopK,
            minScore: request.MinScore,
            moduleName: request.ModuleName,
            ct: ct);

        if (searchResults.Count == 0)
        {
            _logger.LogWarning("[RAG] No relevant chunks found for query");
            return new RagResponse
            {
                Answer = "Bu konuda yeterli bilgiye sahip değilim. İlgili doküman bulunamadı.",
                Sources = []
            };
        }

        // 3. Context oluştur
        var context = string.Join("\n\n---\n\n",
            searchResults.Select((r, i) =>
                $"[Kaynak {i + 1} — {r.Document.SourceType}/{r.Document.ModuleName}, Skor: {r.Score:F2}]\n{r.Document.Content}"));

        var systemPrompt = RagSystemPrompt.Replace("{{context}}", context);

        // 4. LLM'e gönder
        var chatRequest = new ChatRequest
        {
            Messages = [ChatMessage.User(request.Query)],
            SystemPrompt = systemPrompt,
            ModuleName = "RAG",
            Temperature = 0.3f, // Daha deterministik yanıt
            MaxTokens = 2000
        };

        var chatResponse = await _llmService.ChatAsync(chatRequest, ct);

        // 5. Sonuç
        var sources = searchResults.Select(r => new RagSource
        {
            Content = r.Document.Content.Length > 200
                ? r.Document.Content[..200] + "..."
                : r.Document.Content,
            SourceType = r.Document.SourceType,
            SourceId = r.Document.SourceId,
            Score = (float)r.Score
        }).ToList();

        _logger.LogInformation("[RAG] Answer generated. Sources: {Count}, Tokens: {In}/{Out}",
            sources.Count, chatResponse.InputTokens, chatResponse.OutputTokens);

        return new RagResponse
        {
            Answer = chatResponse.Content,
            Sources = sources,
            InputTokens = chatResponse.InputTokens,
            OutputTokens = chatResponse.OutputTokens
        };
    }
}
