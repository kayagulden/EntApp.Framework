using EntApp.Modules.AI.Application.DTOs;

namespace EntApp.Modules.AI.Application.Interfaces;

/// <summary>
/// LLM chat servis arayüzü — provider-agnostic.
/// Arkada Semantic Kernel çalışır.
/// </summary>
public interface ILlmService
{
    /// <summary>Chat completion isteği gönderir.</summary>
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default);
}

/// <summary>
/// Embedding servis arayüzü — metin → vektör dönüşümü.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>Tek metin için embedding üretir.</summary>
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);

    /// <summary>Toplu embedding üretir.</summary>
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default);
}

/// <summary>
/// RAG (Retrieval-Augmented Generation) servis arayüzü.
/// İmplementasyon Faz 9c'de.
/// </summary>
public interface IRagService
{
    /// <summary>Sorguyu embedding'e çevir → benzer chunk bul → LLM'e ver → yanıt döndür.</summary>
    Task<RagResponse> QueryAsync(RagRequest request, CancellationToken ct = default);
}

/// <summary>
/// Prompt şablon yöneticisi — DB'den şablon çekip Scriban ile render eder.
/// </summary>
public interface IPromptManager
{
    /// <summary>Şablonu render eder. Key + model verisi → final prompt metni.</summary>
    Task<string> RenderAsync(string key, object model, CancellationToken ct = default);

    /// <summary>Şablonu DB'den çeker (opsiyonel versiyon).</summary>
    Task<EntApp.Modules.AI.Domain.Entities.PromptTemplate?> GetTemplateAsync(
        string key, int? version = null, CancellationToken ct = default);
}

/// <summary>
/// Doküman işleme arayüzü — PDF, Office → metin → chunk.
/// İmplementasyon Faz 9c'de.
/// </summary>
public interface IDocumentProcessor
{
    /// <summary>Dosyayı okur ve metin parçalarına böler.</summary>
    Task<IReadOnlyList<TextChunk>> ProcessAsync(
        Stream file, string fileName, CancellationToken ct = default);
}
