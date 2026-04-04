using System.Diagnostics;
using EntApp.Modules.AI.Application.DTOs;
using EntApp.Modules.AI.Application.Interfaces;
using EntApp.Modules.AI.Domain.Entities;
using EntApp.Modules.AI.Domain.Enums;
using EntApp.Modules.AI.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace EntApp.Modules.AI.Infrastructure.Services;

/// <summary>
/// Semantic Kernel üzerinden LLM chat completion.
/// Provider-agnostic — DI'den gelen Kernel hangi provider ile yapılandırıldıysa onu kullanır.
/// </summary>
public sealed class SemanticKernelLlmService : ILlmService
{
    private readonly Kernel _kernel;
    private readonly AiDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SemanticKernelLlmService> _logger;

    public SemanticKernelLlmService(
        Kernel kernel,
        AiDbContext dbContext,
        IConfiguration configuration,
        ILogger<SemanticKernelLlmService> logger)
    {
        _kernel = kernel;
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var providerName = _configuration.GetValue<string>("AiSettings:DefaultProvider") ?? "OpenAI";
        var modelName = request.ModelName
            ?? _configuration[$"AiSettings:{providerName}:ChatModel"]
            ?? "gpt-4o-mini";

        try
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            var chatHistory = new ChatHistory();

            // System prompt
            if (!string.IsNullOrEmpty(request.SystemPrompt))
            {
                chatHistory.AddSystemMessage(request.SystemPrompt);
            }

            // Mesaj geçmişi
            foreach (var msg in request.Messages)
            {
                switch (msg.Role.ToLowerInvariant())
                {
                    case "system":
                        chatHistory.AddSystemMessage(msg.Content);
                        break;
                    case "assistant":
                        chatHistory.AddAssistantMessage(msg.Content);
                        break;
                    default:
                        chatHistory.AddUserMessage(msg.Content);
                        break;
                }
            }

            var settings = new PromptExecutionSettings
            {
                ExtensionData = new Dictionary<string, object>
                {
                    ["temperature"] = request.Temperature,
                }
            };

            if (request.MaxTokens.HasValue)
            {
                settings.ExtensionData["max_tokens"] = request.MaxTokens.Value;
            }

            var result = await chatService.GetChatMessageContentAsync(
                chatHistory, settings, _kernel, ct);

            sw.Stop();

            var inputTokens = 0;
            var outputTokens = 0;
            if (result.Metadata?.TryGetValue("Usage", out var usage) == true)
            {
                inputTokens = GetTokenCount(usage, "InputTokenCount");
                outputTokens = GetTokenCount(usage, "OutputTokenCount");
            }

            // Usage log
            var provider = Enum.TryParse<AiProvider>(providerName, out var p) ? p : AiProvider.OpenAI;
            var log = AiUsageLog.CreateSuccess(
                provider, modelName, AiOperation.Chat,
                inputTokens, outputTokens,
                EstimateCost(provider, inputTokens, outputTokens),
                sw.ElapsedMilliseconds,
                moduleName: request.ModuleName);

            _dbContext.AiUsageLogs.Add(log);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation(
                "[AI:Chat] {Provider}/{Model} — {InputTokens}+{OutputTokens} tokens, {Duration}ms",
                providerName, modelName, inputTokens, outputTokens, sw.ElapsedMilliseconds);

            return new ChatResponse
            {
                Content = result.Content ?? string.Empty,
                ModelName = modelName,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                DurationMs = sw.ElapsedMilliseconds,
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            var provider = Enum.TryParse<AiProvider>(providerName, out var p) ? p : AiProvider.OpenAI;

            var log = AiUsageLog.CreateFailure(
                provider, modelName, AiOperation.Chat,
                ex.Message, sw.ElapsedMilliseconds,
                moduleName: request.ModuleName);

            _dbContext.AiUsageLogs.Add(log);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogError(ex, "[AI:Chat] FAILED — {Provider}/{Model}, {Duration}ms",
                providerName, modelName, sw.ElapsedMilliseconds);

            throw;
        }
    }

    private static int GetTokenCount(object? usage, string property)
    {
        if (usage is null) return 0;
        var prop = usage.GetType().GetProperty(property);
        return prop?.GetValue(usage) is int count ? count : 0;
    }

    private static decimal EstimateCost(AiProvider provider, int input, int output) => provider switch
    {
        AiProvider.OpenAI => (input * 0.00015m + output * 0.0006m) / 1000m, // gpt-4o-mini yaklaşık
        AiProvider.Anthropic => (input * 0.003m + output * 0.015m) / 1000m,
        AiProvider.AzureOpenAI => (input * 0.00015m + output * 0.0006m) / 1000m,
        AiProvider.Ollama => 0m, // yerel, ücretsiz
        _ => 0m,
    };
}
