using EntApp.Modules.StateFlow.Application.Dtos;

namespace EntApp.Modules.StateFlow.Application.Interfaces;

/// <summary>
/// State machine runtime motoru interface'i.
/// DB'den flow tanımı yükler, Stateless kütüphanesi ile in-memory makine kurar,
/// geçiş validate eder ve tetikler.
/// </summary>
public interface IStateFlowEngine
{
    /// <summary>Belirtilen geçişin geçerli olup olmadığını kontrol eder.</summary>
    Task<bool> ValidateTransitionAsync(
        string entityType, string currentState, string trigger,
        Guid flowDefinitionId, CancellationToken ct = default);

    /// <summary>Mevcut state'den yapılabilecek tüm geçişleri döndürür.</summary>
    Task<IReadOnlyList<TriggerInfo>> GetAllowedTriggersAsync(
        string entityType, string currentState,
        Guid flowDefinitionId, CancellationToken ct = default);

    /// <summary>Geçişi tetikler ve yeni state adını döndürür.</summary>
    Task<string> FireTransitionAsync(
        string entityType, string currentState, string trigger,
        Guid flowDefinitionId, CancellationToken ct = default);
}
