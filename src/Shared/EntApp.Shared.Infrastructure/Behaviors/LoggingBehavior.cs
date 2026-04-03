using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.Behaviors;

/// <summary>
/// MediatR pipeline behavior — request/response loglama.
/// Request adını, parametrelerini ve süresini loglar.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("[START] {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            _logger.LogInformation("[END] {RequestName} completed in {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "[ERROR] {RequestName} failed after {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
