using EntApp.Shared.Kernel.Results;
using FluentValidation;
using MediatR;

namespace EntApp.Shared.Infrastructure.Behaviors;

/// <summary>
/// MediatR pipeline behavior — FluentValidation entegrasyonu.
/// Request'e ait tüm IValidator'ları çalıştırır.
/// Hata varsa Result.ValidationFailure döner, handler'a ulaşmaz.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToArray();

        if (errors.Length != 0)
        {
            return CreateValidationResult(errors);
        }

        return await next();
    }

    private static TResponse CreateValidationResult(Error[] errors)
    {
        // Result<T> → ValidationFailure
        if (typeof(TResponse).IsGenericType
            && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeof(TResponse).GetGenericArguments()[0];
            var validationFailureMethod = typeof(Result<>)
                .MakeGenericType(valueType)
                .GetMethod(nameof(Result<object>.ValidationFailure))!;

            return (TResponse)validationFailureMethod.Invoke(null, [errors])!;
        }

        // Result (non-generic) → ilk hatayı döndür
        return (TResponse)(object)Result.Failure(errors[0]);
    }
}
