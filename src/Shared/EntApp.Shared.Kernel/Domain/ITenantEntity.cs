namespace EntApp.Shared.Kernel.Domain;

/// <summary>
/// Multi-tenant entity'ler bu interface'i implement eder.
/// EF Core global query filter ile otomatik filtreleme sağlar.
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; }
}
