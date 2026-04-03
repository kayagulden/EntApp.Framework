namespace EntApp.Shared.Kernel.Exceptions;

/// <summary>
/// Kayıt bulunamadığında fırlatılır.
/// ExceptionHandlingMiddleware tarafından 404 ProblemDetails'e çevrilir.
/// </summary>
public sealed class NotFoundException : DomainException
{
    public string EntityName { get; }

    public object EntityId { get; }

    public NotFoundException(string entityName, object entityId)
        : base($"{entityName} with id '{entityId}' was not found.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}
