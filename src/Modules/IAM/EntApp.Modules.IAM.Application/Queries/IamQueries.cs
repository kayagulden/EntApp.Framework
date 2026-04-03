using EntApp.Modules.IAM.Application.DTOs;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Kernel.Results;
using MediatR;

namespace EntApp.Modules.IAM.Application.Queries;

/// <summary>ID'ye göre kullanıcı getir.</summary>
public sealed record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserDto>>;

/// <summary>Sayfalanmış kullanıcı listesi.</summary>
public sealed record GetUsersPagedQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null) : IRequest<Result<PagedResult<UserDto>>>;

/// <summary>Tüm rolleri getir.</summary>
public sealed record GetRolesQuery() : IRequest<Result<IReadOnlyList<RoleDto>>>;

/// <summary>Organizasyon ağacını getir.</summary>
public sealed record GetOrganizationTreeQuery() : IRequest<Result<IReadOnlyList<OrganizationDto>>>;
