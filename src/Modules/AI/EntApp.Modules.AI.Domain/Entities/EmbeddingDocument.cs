using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.AI.Domain.Entities;

/// <summary>
/// Embedding doküman parçası — RAG pipeline'da kullanılır.
/// PDF/Office → metin → chunk → embedding → pgvector.
/// Embedding vektör alanı Faz 9b'de pgvector ile eklenecek.
/// </summary>
public sealed class EmbeddingDocument : AuditableEntity<Guid>, ITenantEntity
{
    /// <summary>Kaynak modül (ör: "CRM", "HR")</summary>
    public string ModuleName { get; private set; } = string.Empty;

    /// <summary>Kaynak tipi (ör: "PDF", "Invoice", "Contract")</summary>
    public string SourceType { get; private set; } = string.Empty;

    /// <summary>Kaynak entity ID</summary>
    public string? SourceId { get; private set; }

    /// <summary>Düz metin içerik (chunk)</summary>
    public string Content { get; private set; } = string.Empty;

    /// <summary>Chunk sırası (0-based)</summary>
    public int ChunkIndex { get; private set; }

    /// <summary>Ek metadata (JSON)</summary>
    public string? Metadata { get; private set; }

    /// <summary>Token sayısı</summary>
    public int TokenCount { get; private set; }

    // Embedding alanı Faz 9b'de pgvector ile eklenecek:
    // public Vector Embedding { get; private set; }

    public Guid TenantId { get; set; }

    private EmbeddingDocument() { }

    public static EmbeddingDocument Create(
        string moduleName,
        string sourceType,
        string content,
        int chunkIndex,
        int tokenCount,
        string? sourceId = null,
        string? metadata = null)
    {
        return new EmbeddingDocument
        {
            Id = Guid.NewGuid(),
            ModuleName = moduleName,
            SourceType = sourceType,
            SourceId = sourceId,
            Content = content,
            ChunkIndex = chunkIndex,
            TokenCount = tokenCount,
            Metadata = metadata,
        };
    }
}
