using EntApp.Modules.AI.Domain.Enums;
using EntApp.Modules.AI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.AI.Infrastructure.Endpoints;

/// <summary>
/// AI kullanım istatistikleri ve maliyet takibi endpoint'leri.
/// </summary>
public static class UsageEndpoints
{
    public static IEndpointRouteBuilder MapUsageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai/usage")
            .WithTags("AI Usage");

        // ── Dashboard / Summary ──────────────────────────
        group.MapGet("/summary", async (AiDbContext db, int days = 30) =>
        {
            var since = DateTime.UtcNow.AddDays(-days);

            var logs = await db.AiUsageLogs
                .Where(l => l.CreatedAt >= since)
                .ToListAsync();

            var totalCost = logs.Sum(l => l.EstimatedCost);
            var totalTokens = logs.Sum(l => l.TotalTokens);
            var totalCalls = logs.Count;
            var successRate = totalCalls > 0
                ? (double)logs.Count(l => l.IsSuccess) / totalCalls * 100
                : 0;

            var byOperation = logs
                .GroupBy(l => l.Operation)
                .Select(g => new
                {
                    Operation = g.Key.ToString(),
                    Calls = g.Count(),
                    Tokens = g.Sum(l => l.TotalTokens),
                    Cost = g.Sum(l => l.EstimatedCost),
                    AvgDurationMs = (long)g.Average(l => l.DurationMs)
                })
                .OrderByDescending(x => x.Cost)
                .ToList();

            var byProvider = logs
                .GroupBy(l => l.Provider)
                .Select(g => new
                {
                    Provider = g.Key.ToString(),
                    Calls = g.Count(),
                    Tokens = g.Sum(l => l.TotalTokens),
                    Cost = g.Sum(l => l.EstimatedCost)
                })
                .OrderByDescending(x => x.Cost)
                .ToList();

            var byModel = logs
                .GroupBy(l => l.ModelName)
                .Select(g => new
                {
                    Model = g.Key,
                    Calls = g.Count(),
                    Tokens = g.Sum(l => l.TotalTokens),
                    Cost = g.Sum(l => l.EstimatedCost),
                    AvgDurationMs = (long)g.Average(l => l.DurationMs)
                })
                .OrderByDescending(x => x.Cost)
                .ToList();

            return Results.Ok(new
            {
                period = $"Last {days} days",
                totalCalls,
                totalTokens,
                totalCost,
                successRate = Math.Round(successRate, 1),
                byOperation,
                byProvider,
                byModel
            });
        })
        .WithName("UsageSummary")
        .WithSummary("AI kullanım özeti — maliyet, token, süre istatistikleri");

        // ── Daily breakdown ──────────────────────────────
        group.MapGet("/daily", async (AiDbContext db, int days = 30) =>
        {
            var since = DateTime.UtcNow.AddDays(-days);

            var daily = await db.AiUsageLogs
                .Where(l => l.CreatedAt >= since)
                .GroupBy(l => l.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Calls = g.Count(),
                    Tokens = g.Sum(l => l.TotalTokens),
                    Cost = g.Sum(l => l.EstimatedCost),
                    Errors = g.Count(l => !l.IsSuccess)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return Results.Ok(daily);
        })
        .WithName("UsageDaily")
        .WithSummary("Günlük AI kullanım dağılımı");

        // ── Recent logs ──────────────────────────────────
        group.MapGet("/logs", async (AiDbContext db, int page = 1, int pageSize = 20,
            string? operation = null, bool? success = null) =>
        {
            var query = db.AiUsageLogs.AsQueryable();

            if (!string.IsNullOrEmpty(operation) && Enum.TryParse<AiOperation>(operation, out var op))
                query = query.Where(l => l.Operation == op);

            if (success.HasValue)
                query = query.Where(l => l.IsSuccess == success.Value);

            var total = await query.CountAsync();
            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    Provider = l.Provider.ToString(),
                    l.ModelName,
                    Operation = l.Operation.ToString(),
                    l.InputTokens,
                    l.OutputTokens,
                    l.TotalTokens,
                    l.EstimatedCost,
                    l.DurationMs,
                    l.IsSuccess,
                    l.ErrorMessage,
                    l.ModuleName,
                    l.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(new { items = logs, totalCount = total, pageNumber = page, pageSize });
        })
        .WithName("UsageLogs")
        .WithSummary("AI kullanım logları (sayfalı)");

        return app;
    }
}
