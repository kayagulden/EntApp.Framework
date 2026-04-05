using EntApp.Modules.CRM.Application.DTOs;
using EntApp.Modules.CRM.Application.Queries;
using EntApp.Modules.CRM.Domain.Enums;
using EntApp.Modules.CRM.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.CRM.Infrastructure.Handlers.Queries;

public sealed class ListCustomersQueryHandler(CrmDbContext db)
    : IRequestHandler<ListCustomersQuery, PagedResult<CustomerListDto>>
{
    public async Task<PagedResult<CustomerListDto>> Handle(ListCustomersQuery request, CancellationToken ct)
    {
        var query = db.Customers.Where(c => c.IsActive);
        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(c => c.Name.Contains(request.Search) || (c.Code != null && c.Code.Contains(request.Search)));
        if (!string.IsNullOrEmpty(request.Segment) && Enum.TryParse<CustomerSegment>(request.Segment, out var seg))
            query = query.Where(c => c.Segment == seg);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.Name)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(c => new CustomerListDto(c.Id.Value, c.Name, c.Code, c.Email, c.Phone,
                c.City, c.Country, c.CustomerType.ToString(), c.Segment.ToString(), c.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<CustomerListDto>
        { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}

public sealed class GetCustomerQueryHandler(CrmDbContext db)
    : IRequestHandler<GetCustomerQuery, object?>
{
    public async Task<object?> Handle(GetCustomerQuery request, CancellationToken ct)
    {
        return await db.Customers.Include(x => x.Contacts).Include(x => x.Opportunities)
            .FirstOrDefaultAsync(x => x.Id.Value == request.Id, ct);
    }
}

public sealed class ListContactsQueryHandler(CrmDbContext db)
    : IRequestHandler<ListContactsQuery, PagedResult<ContactListDto>>
{
    public async Task<PagedResult<ContactListDto>> Handle(ListContactsQuery request, CancellationToken ct)
    {
        var query = db.Contacts.AsQueryable();
        if (request.CustomerId.HasValue) query = query.Where(c => c.CustomerId.Value == request.CustomerId.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.LastName)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(c => new ContactListDto(c.Id.Value, c.CustomerId.Value, c.FirstName, c.LastName,
                c.Title, c.Email, c.Phone, c.IsPrimary))
            .ToListAsync(ct);

        return new PagedResult<ContactListDto>
        { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}

public sealed class ListOpportunitiesQueryHandler(CrmDbContext db)
    : IRequestHandler<ListOpportunitiesQuery, PagedResult<OpportunityListDto>>
{
    public async Task<PagedResult<OpportunityListDto>> Handle(ListOpportunitiesQuery request, CancellationToken ct)
    {
        var query = db.Opportunities.Include(o => o.Customer).AsQueryable();
        if (!string.IsNullOrEmpty(request.Stage) && Enum.TryParse<OpportunityStage>(request.Stage, out var s))
            query = query.Where(o => o.Stage == s);
        if (request.CustomerId.HasValue) query = query.Where(o => o.CustomerId.Value == request.CustomerId.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(o => new OpportunityListDto(o.Id.Value, o.Title, o.Customer.Name,
                o.EstimatedValue, o.Currency, o.Stage.ToString(),
                o.Probability, o.ExpectedCloseDate, o.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<OpportunityListDto>
        { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}

public sealed class GetPipelineQueryHandler(CrmDbContext db)
    : IRequestHandler<GetPipelineQuery, List<PipelineStageDto>>
{
    public async Task<List<PipelineStageDto>> Handle(GetPipelineQuery request, CancellationToken ct)
    {
        return await db.Opportunities
            .GroupBy(o => o.Stage)
            .Select(g => new PipelineStageDto(g.Key.ToString(), g.Count(), g.Sum(o => o.EstimatedValue)))
            .OrderBy(x => x.Stage)
            .ToListAsync(ct);
    }
}

public sealed class ListActivitiesQueryHandler(CrmDbContext db)
    : IRequestHandler<ListActivitiesQuery, PagedResult<ActivityListDto>>
{
    public async Task<PagedResult<ActivityListDto>> Handle(ListActivitiesQuery request, CancellationToken ct)
    {
        var query = db.Activities.AsQueryable();
        if (request.CustomerId.HasValue)
            query = query.Where(a => a.CustomerId.HasValue && a.CustomerId.Value.Value == request.CustomerId.Value);
        if (!string.IsNullOrEmpty(request.Type) && Enum.TryParse<ActivityType>(request.Type, out var at))
            query = query.Where(a => a.ActivityType == at);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(a => new ActivityListDto(a.Id.Value, a.Subject, a.ActivityType.ToString(),
                a.Status.ToString(), a.CustomerId.HasValue ? a.CustomerId.Value.Value : (Guid?)null,
                a.DueDate, a.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ActivityListDto>
        { Items = items, TotalCount = total, PageNumber = request.Page, PageSize = request.PageSize };
    }
}
