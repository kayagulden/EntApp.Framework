namespace EntApp.Shared.Kernel.Exceptions;

/// <summary>
/// İş kuralı ihlali exception'ı.
/// Domain katmanında, Result Pattern kullanılamayacak
/// kritik ihlallerde fırlatılır.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
