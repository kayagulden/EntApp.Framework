using EntApp.Shared.Contracts.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.Behaviors;

/// <summary>
/// MediatR pipeline behavior — Command'lar için otomatik transaction yönetimi.
/// Begin → Handler → Commit (veya Rollback).
/// ITransactionless marker interface ile opt-out edilebilir.
/// Query'ler (IRequest&lt;Result&lt;T&gt;&gt; tipi) için transaction açılmaz.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(IUnitOfWork unitOfWork, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        // ITransactionless marker ile opt-out
        if (request is ITransactionless)
        {
            return await next();
        }

        // Command isimleri genelde "Command" ile biter
        var requestName = typeof(TRequest).Name;
        if (!requestName.EndsWith("Command", StringComparison.OrdinalIgnoreCase))
        {
            return await next();
        }

        _logger.LogDebug("[TX:BEGIN] {RequestName}", requestName);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogDebug("[TX:COMMIT] {RequestName}", requestName);

            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            _logger.LogWarning("[TX:ROLLBACK] {RequestName}", requestName);

            throw;
        }
    }
}
