namespace EntApp.Shared.Kernel.Results;

/// <summary>
/// Domain hata bilgisini taşıyan immutable record.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    /// <summary>Hata yok — başarılı sonuç.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string code, string message)
        => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message)
        => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message)
        => new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string code, string message)
        => new(code, message, ErrorType.Unauthorized);

    public static Error Failure(string code, string message)
        => new(code, message, ErrorType.Failure);
}
