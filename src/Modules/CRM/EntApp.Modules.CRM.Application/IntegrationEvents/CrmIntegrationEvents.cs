using EntApp.Shared.Contracts.Events;

namespace EntApp.Modules.CRM.Application.IntegrationEvents;

/// <summary>Fırsat kazanıldığında yayınlanır → Sales sipariş oluşturabilir.</summary>
public sealed record OpportunityWonEvent(
    Guid OpportunityId,
    string Title,
    Guid CustomerId,
    string? CustomerName,
    decimal EstimatedValue,
    string Currency) : IntegrationEvent;

/// <summary>Fırsat kaybedildiğinde yayınlanır.</summary>
public sealed record OpportunityLostEvent(
    Guid OpportunityId,
    string Title,
    Guid CustomerId,
    string? LostReason) : IntegrationEvent;

/// <summary>Yeni müşteri oluşturulduğunda yayınlanır → Finance cari hesap oluşturabilir.</summary>
public sealed record CustomerCreatedEvent(
    Guid CustomerId,
    string Name,
    string CustomerType,
    string? Email,
    string? TaxNumber) : IntegrationEvent;
