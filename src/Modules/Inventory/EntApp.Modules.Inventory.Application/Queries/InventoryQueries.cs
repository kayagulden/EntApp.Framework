using EntApp.Shared.Contracts.Common;
using MediatR;

namespace EntApp.Modules.Inventory.Application.Queries;

public sealed record ListProductsQuery(string? Search, string? Category, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<object>>;
public sealed record GetProductQuery(Guid Id) : IRequest<object?>;
public sealed record ListWarehousesQuery() : IRequest<List<object>>;
public sealed record ListStockMovementsQuery(Guid? ProductId, Guid? WarehouseId, string? Type,
    int Page = 1, int PageSize = 20) : IRequest<PagedResult<object>>;
public sealed record GetStockBalanceQuery(Guid? WarehouseId) : IRequest<object>;
public sealed record GetStockAlertsQuery() : IRequest<object>;
