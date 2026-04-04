using EntApp.Modules.AI.Application.Interfaces;
using EntApp.Modules.AI.Domain.Entities;
using EntApp.Modules.AI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scriban;

namespace EntApp.Modules.AI.Infrastructure.Services;

/// <summary>
/// Scriban template engine ile prompt render servisi.
/// DB'den PromptTemplate çeker, Scriban ile değişkenleri doldurur.
/// </summary>
public sealed class ScribanPromptManager : IPromptManager
{
    private readonly AiDbContext _dbContext;
    private readonly ILogger<ScribanPromptManager> _logger;

    public ScribanPromptManager(AiDbContext dbContext, ILogger<ScribanPromptManager> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string> RenderAsync(string key, object model, CancellationToken ct = default)
    {
        var template = await GetTemplateAsync(key, null, ct)
            ?? throw new KeyNotFoundException($"Prompt template '{key}' not found.");

        var scribanTemplate = Template.Parse(template.TemplateContent);

        if (scribanTemplate.HasErrors)
        {
            var errors = string.Join("; ", scribanTemplate.Messages.Select(m => m.Message));
            _logger.LogError("[AI:Prompt] Template parse error for '{Key}': {Errors}", key, errors);
            throw new InvalidOperationException($"Template parse error: {errors}");
        }

        var result = await scribanTemplate.RenderAsync(model);

        _logger.LogDebug("[AI:Prompt] Rendered '{Key}' v{Version} — {Length} chars",
            key, template.Version, result.Length);

        return result;
    }

    public async Task<PromptTemplate?> GetTemplateAsync(
        string key, int? version = null, CancellationToken ct = default)
    {
        var query = _dbContext.PromptTemplates
            .Where(t => t.Key == key && t.IsActive);

        if (version.HasValue)
        {
            query = query.Where(t => t.Version == version.Value);
        }
        else
        {
            // En yüksek versiyon
            query = query.OrderByDescending(t => t.Version);
        }

        return await query.FirstOrDefaultAsync(ct);
    }
}
