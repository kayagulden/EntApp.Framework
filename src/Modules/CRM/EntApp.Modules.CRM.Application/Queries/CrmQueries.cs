using EntApp.Modules.CRM.Application.DTOs;
using EntApp.Shared.Contracts.Common;
using MediatR;

namespace EntApp.Modules.CRM.Application.Queries;

public sealed record ListCustomersQuery(string? Search, string? Segment, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<CustomerListDto>>;

public sealed record GetCustomerQuery(Guid Id) : IRequest<object?>;

public sealed record ListContactsQuery(Guid? CustomerId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ContactListDto>>;

public sealed record ListOpportunitiesQuery(string? Stage, Guid? CustomerId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<OpportunityListDto>>;

public sealed record GetPipelineQuery() : IRequest<List<PipelineStageDto>>;

public sealed record ListActivitiesQuery(Guid? CustomerId, string? Type, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ActivityListDto>>;
