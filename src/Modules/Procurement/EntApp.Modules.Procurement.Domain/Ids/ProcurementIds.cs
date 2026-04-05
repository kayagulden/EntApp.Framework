using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Procurement.Domain.Ids;

public readonly record struct SupplierId(Guid Value) : IEntityId;
public readonly record struct PurchaseRequestId(Guid Value) : IEntityId;
public readonly record struct PurchaseOrderId(Guid Value) : IEntityId;
