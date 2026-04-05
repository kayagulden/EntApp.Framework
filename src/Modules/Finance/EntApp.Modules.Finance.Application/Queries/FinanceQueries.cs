using EntApp.Modules.Finance.Application.DTOs;
using EntApp.Shared.Contracts.Common;
using MediatR;

namespace EntApp.Modules.Finance.Application.Queries;

public sealed record ListAccountsQuery(string? Search, string? Type, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<AccountListDto>>;
public sealed record GetAccountQuery(Guid Id) : IRequest<object?>;
public sealed record GetBalanceSummaryQuery() : IRequest<List<BalanceSummaryDto>>;
public sealed record ListInvoicesQuery(string? Type, string? Status, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<InvoiceListDto>>;
public sealed record GetInvoiceQuery(Guid Id) : IRequest<object?>;
public sealed record GetOverdueInvoicesQuery() : IRequest<List<OverdueInvoiceDto>>;
public sealed record ListPaymentsQuery(Guid? AccountId, string? Direction, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<PaymentListDto>>;
