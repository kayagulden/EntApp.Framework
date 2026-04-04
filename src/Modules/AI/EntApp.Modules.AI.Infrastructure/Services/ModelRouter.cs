using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.AI.Infrastructure.Services;

/// <summary>
/// Akıllı model yönlendirmesi — istek karmaşıklığına göre uygun modeli seç.
/// Basit iş → küçük/ucuz model, karmaşık iş → güçlü model.
/// </summary>
public sealed class ModelRouter
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ModelRouter> _logger;

    /// <summary>Token eşik değeri — bu altı basit, üstü karmaşık.</summary>
    private readonly int _complexityThreshold;

    public ModelRouter(IConfiguration configuration, ILogger<ModelRouter> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _complexityThreshold = configuration.GetValue("AiSettings:ModelRouting:ComplexityThreshold", 500);
    }

    /// <summary>
    /// İstek içeriğine göre en uygun modeli seç.
    /// </summary>
    /// <param name="inputText">İstek metni</param>
    /// <param name="preferredModel">Kullanıcının tercih ettiği model (null ise otomatik)</param>
    /// <returns>Seçilen model adı</returns>
    public string SelectModel(string inputText, string? preferredModel = null)
    {
        // Kullanıcı model belirttiyse onu kullan
        if (!string.IsNullOrEmpty(preferredModel))
            return preferredModel;

        var provider = _configuration.GetValue<string>("AiSettings:DefaultProvider") ?? "OpenAI";
        var estimatedTokens = inputText.Length / 4;
        var isComplex = estimatedTokens > _complexityThreshold
                        || ContainsComplexPatterns(inputText);

        string selectedModel;

        if (isComplex)
        {
            selectedModel = _configuration[$"AiSettings:{provider}:ChatModel"] ?? "gpt-4o-mini";
            _logger.LogDebug("[AI:Router] Complex request ({Tokens} tokens) → {Model}",
                estimatedTokens, selectedModel);
        }
        else
        {
            selectedModel = _configuration[$"AiSettings:{provider}:LiteModel"]
                            ?? _configuration[$"AiSettings:{provider}:ChatModel"]
                            ?? "gpt-4o-mini";
            _logger.LogDebug("[AI:Router] Simple request ({Tokens} tokens) → {Model}",
                estimatedTokens, selectedModel);
        }

        return selectedModel;
    }

    private static bool ContainsComplexPatterns(string text)
    {
        // Karmaşıklık göstergeleri
        var lowerText = text.ToLowerInvariant();
        return lowerText.Contains("analiz")
               || lowerText.Contains("analysis")
               || lowerText.Contains("compare")
               || lowerText.Contains("karşılaştır")
               || lowerText.Contains("özetle")
               || lowerText.Contains("summarize")
               || lowerText.Contains("code")
               || lowerText.Contains("sql")
               || lowerText.Contains("explain");
    }
}
