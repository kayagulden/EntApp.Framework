using EntApp.Shared.Contracts.Events;

namespace EntApp.Modules.Finance.Application.IntegrationEvents;

/// <summary>Fatura onaylandığında yayınlanır.</summary>
public sealed record InvoiceApprovedEvent(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid AccountId,
    string InvoiceType,
    decimal GrandTotal,
    string Currency,
    DateTime DueDate) : IntegrationEvent;

/// <summary>Ödeme yapıldığında yayınlanır.</summary>
public sealed record PaymentReceivedEvent(
    Guid PaymentId,
    Guid AccountId,
    Guid? InvoiceId,
    decimal Amount,
    string Currency,
    string PaymentMethod) : IntegrationEvent;
