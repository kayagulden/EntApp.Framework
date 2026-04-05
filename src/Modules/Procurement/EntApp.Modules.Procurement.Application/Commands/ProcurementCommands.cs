using MediatR;

namespace EntApp.Modules.Procurement.Application.Commands;

public sealed record CreateSupplierCommand(string Code, string Name, string? Email = null,
    string? Phone = null, string? Address = null, string? TaxNumber = null,
    string? ContactPerson = null, int PaymentTermDays = 30) : IRequest<Guid>;
public sealed record RateSupplierCommand(Guid SupplierId, string Rating) : IRequest<string>;
public sealed record CreatePurchaseRequestCommand(string RequestNumber, Guid RequestedByUserId,
    string? Department = null, string? Description = null, string? ItemsJson = null,
    decimal EstimatedTotal = 0, string? Currency = null, DateTime? RequiredByDate = null) : IRequest<Guid>;
public sealed record ApprovePurchaseRequestCommand(Guid RequestId) : IRequest<string>;
public sealed record RejectPurchaseRequestCommand(Guid RequestId) : IRequest<string>;
public sealed record CreatePurchaseOrderCommand(string OrderNumber, Guid SupplierId,
    DateTime OrderDate = default, string? SupplierName = null, string? Currency = null,
    DateTime? ExpectedDeliveryDate = null, string? ItemsJson = null,
    decimal SubTotal = 0, decimal TaxTotal = 0, string? Notes = null) : IRequest<Guid>;
public sealed record ReceivePurchaseOrderCommand(Guid OrderId, bool Full = false, decimal Amount = 0) : IRequest<ReceiveResult>;
public sealed record ReceiveResult(Guid Id, string Status, decimal ReceivedTotal);
public sealed record MatchInvoiceCommand(Guid OrderId, Guid InvoiceId) : IRequest<string>;
