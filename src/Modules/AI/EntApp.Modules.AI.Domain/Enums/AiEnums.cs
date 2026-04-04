namespace EntApp.Modules.AI.Domain.Enums;

/// <summary>LLM sağlayıcı.</summary>
public enum AiProvider
{
    OpenAI = 0,
    Anthropic = 1,
    AzureOpenAI = 2,
    Ollama = 3
}

/// <summary>Model tipi.</summary>
public enum AiModelType
{
    Chat = 0,
    Embedding = 1,
    Vision = 2
}

/// <summary>AI operasyon tipi — usage log için.</summary>
public enum AiOperation
{
    Chat = 0,
    Embedding = 1,
    RAG = 2,
    Vision = 3
}
