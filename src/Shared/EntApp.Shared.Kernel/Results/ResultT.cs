namespace EntApp.Shared.Kernel.Results;

/// <summary>
/// Değer taşıyan işlem sonucu.
/// Başarılı ise Value, başarısız ise Error(s) döner.
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Başarılı sonucun değeri.
    /// Başarısız sonuçta erişilirse InvalidOperationException fırlatılır.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");

    /// <summary>
    /// Birden fazla validasyon hatası.
    /// </summary>
    public Error[] Errors { get; private init; } = [];

    public static Result<T> Success(T value) => new(value, true, Error.None);

    public new static Result<T> Failure(Error error) => new(default, false, error);

    /// <summary>
    /// Birden fazla validasyon hatası ile başarısız sonuç.
    /// FluentValidation entegrasyonu için.
    /// </summary>
    public static Result<T> ValidationFailure(Error[] errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Length == 0)
        {
            throw new ArgumentException("Validation failure must have at least one error.", nameof(errors));
        }

        return new Result<T>(default, false, errors[0])
        {
            Errors = errors
        };
    }

    /// <summary>
    /// T → Result&lt;T&gt; implicit dönüşüm.
    /// Handler'larda <c>return myValue;</c> yazmayı mümkün kılar.
    /// </summary>
    public static implicit operator Result<T>(T? value)
        => value is not null ? Success(value) : Failure(Error.Failure("Result.NullValue", "Value cannot be null."));
}
