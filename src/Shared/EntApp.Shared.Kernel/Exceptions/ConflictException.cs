namespace EntApp.Shared.Kernel.Exceptions;

/// <summary>
/// Çakışma durumunda fırlatılır (duplicate, concurrency vb.).
/// ExceptionHandlingMiddleware tarafından 409 ProblemDetails'e çevrilir.
/// </summary>
public sealed class ConflictException : DomainException
{
    public ConflictException(string message)
        : base(message) { }

    public ConflictException(string entityName, object entityId)
        : base($"{entityName} with id '{entityId}' already exists or has a conflict.") { }
}
