using EntApp.Shared.Contracts.Common;
using MediatR;

namespace EntApp.Modules.Sales.Application.Queries;

public sealed record ListPriceListsQuery() : IRequest<List<object>>;
public sealed record ListOrdersQuery(string? Status, Guid? CustomerId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<object>>;
public sealed record GetOrderQuery(Guid Id) : IRequest<object?>;
public sealed record GetSalesDashboardQuery() : IRequest<object>;
