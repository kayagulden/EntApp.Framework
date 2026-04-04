namespace EntApp.Modules.AI.Application.DTOs;

/// <summary>Chat isteği.</summary>
public sealed class ChatRequest
{
    /// <summary>Mesaj geçmişi</summary>
    public IReadOnlyList<ChatMessage> Messages { get; init; } = [];

    /// <summary>Kullanılacak model adı (null ise default)</summary>
    public string? ModelName { get; init; }

    /// <summary>Temperature (0-2, default: 0.7)</summary>
    public float Temperature { get; init; } = 0.7f;

    /// <summary>Maksimum token (null ise model default)</summary>
    public int? MaxTokens { get; init; }

    /// <summary>Sistem promptu</summary>
    public string? SystemPrompt { get; init; }

    /// <summary>Çağrıyı yapan modül adı (loglama için)</summary>
    public string? ModuleName { get; init; }
}

/// <summary>Chat mesajı.</summary>
public sealed class ChatMessage
{
    public string Role { get; init; } = "user";
    public string Content { get; init; } = string.Empty;

    public static ChatMessage User(string content) => new() { Role = "user", Content = content };
    public static ChatMessage Assistant(string content) => new() { Role = "assistant", Content = content };
    public static ChatMessage System(string content) => new() { Role = "system", Content = content };
}

/// <summary>Chat yanıtı.</summary>
public sealed class ChatResponse
{
    public string Content { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public long DurationMs { get; init; }
}

/// <summary>RAG isteği.</summary>
public sealed class RagRequest
{
    public string Query { get; init; } = string.Empty;
    public string? ModuleName { get; init; }
    public int TopK { get; init; } = 5;
    public float MinScore { get; init; } = 0.7f;
}

/// <summary>RAG yanıtı.</summary>
public sealed class RagResponse
{
    public string Answer { get; init; } = string.Empty;
    public IReadOnlyList<RagSource> Sources { get; init; } = [];
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
}

/// <summary>RAG kaynak referansı.</summary>
public sealed class RagSource
{
    public string Content { get; init; } = string.Empty;
    public string SourceType { get; init; } = string.Empty;
    public string? SourceId { get; init; }
    public float Score { get; init; }
}

/// <summary>Metin parçası (chunking sonucu).</summary>
public sealed class TextChunk
{
    public string Content { get; init; } = string.Empty;
    public int Index { get; init; }
    public int TokenCount { get; init; }
}
