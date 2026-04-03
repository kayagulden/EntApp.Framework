using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.Behaviors;

/// <summary>
/// MediatR pipeline behavior — performans izleme.
/// 500ms'den uzun süren request'ler için warning log üretir.
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private const int WarningThresholdMs = 500;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > WarningThresholdMs)
        {
            _logger.LogWarning(
                "[SLOW] {RequestName} took {ElapsedMs}ms (threshold: {Threshold}ms). Request: {@Request}",
                typeof(TRequest).Name,
                stopwatch.ElapsedMilliseconds,
                WarningThresholdMs,
                request);
        }

        return response;
    }
}
