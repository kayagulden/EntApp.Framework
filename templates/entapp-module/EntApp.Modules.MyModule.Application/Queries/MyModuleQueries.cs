using EntApp.Shared.Contracts.Common;
using MediatR;

namespace EntApp.Modules.MyModule.Application.Queries;

/// <summary>
/// Modül query tanımları — her IRequest bir MediatR query'dir.
/// Handler'lar Infrastructure/Handlers altında implemente edilir.
/// </summary>

public sealed record ListSampleEntitiesQuery(
    string? Search = null, int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<SampleEntityListItem>>;
public sealed record SampleEntityListItem(
    Guid Id, string Name, string? Description,
    string Status, DateTime CreatedAt);

public sealed record GetSampleEntityQuery(Guid Id) : IRequest<object?>;
