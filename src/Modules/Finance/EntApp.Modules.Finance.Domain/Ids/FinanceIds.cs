using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Finance.Domain.Ids;

public readonly record struct AccountId(Guid Value) : IEntityId;
public readonly record struct InvoiceId(Guid Value) : IEntityId;
public readonly record struct InvoiceItemId(Guid Value) : IEntityId;
public readonly record struct PaymentId(Guid Value) : IEntityId;
