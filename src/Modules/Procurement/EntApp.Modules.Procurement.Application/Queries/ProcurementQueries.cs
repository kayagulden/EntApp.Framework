using EntApp.Shared.Contracts.Common;
using MediatR;

namespace EntApp.Modules.Procurement.Application.Queries;

public sealed record ListSuppliersQuery(string? Search, string? Rating, int Page = 1, int PageSize = 20) : IRequest<PagedResult<object>>;
public sealed record ListPurchaseRequestsQuery(string? Status, int Page = 1, int PageSize = 20) : IRequest<PagedResult<object>>;
public sealed record ListPurchaseOrdersQuery(string? Status, Guid? SupplierId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<object>>;
public sealed record GetPurchaseOrderQuery(Guid Id) : IRequest<object?>;
