using MediatR;

namespace EntApp.Modules.Finance.Application.Commands;

public sealed record CreateAccountCommand(string Code, string Name, string AccountType = "Customer",
    string? Currency = null, string? TaxNumber = null, string? Email = null,
    string? Phone = null, string? Address = null) : IRequest<Guid>;

public sealed record CreateInvoiceCommand(string InvoiceNumber, Guid AccountId,
    string InvoiceType = "Sales", DateTime InvoiceDate = default, DateTime DueDate = default,
    string? Currency = null, string? Notes = null,
    List<CreateInvoiceItemDto>? Items = null) : IRequest<CreateInvoiceResult>;
public sealed record CreateInvoiceItemDto(string Description, decimal Quantity = 1,
    decimal UnitPrice = 0, decimal TaxRate = 20, decimal DiscountRate = 0);
public sealed record CreateInvoiceResult(Guid Id, string InvoiceNumber, decimal GrandTotal);

public sealed record ApproveInvoiceCommand(Guid InvoiceId) : IRequest<string>;

public sealed record CreatePaymentCommand(Guid AccountId, decimal Amount,
    string Direction = "Incoming", string Method = "BankTransfer",
    DateTime? PaymentDate = null, Guid? InvoiceId = null,
    string? Currency = null, string? ReferenceNumber = null, string? Notes = null) : IRequest<Guid>;
