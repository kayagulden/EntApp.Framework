using EntApp.Modules.IAM.Application.DTOs;
using EntApp.Modules.IAM.Application.Queries;
using EntApp.Modules.IAM.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.IAM.Infrastructure.Handlers;

// ─── GetUserById ────────────────────────────────────────────
public sealed class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IamDbContext _db;

    public GetUserByIdHandler(IamDbContext db) => _db = db;

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result<UserDto>.Failure(Error.NotFound("User.NotFound", "Kullanıcı bulunamadı."));
        }

        return Result<UserDto>.Success(MapToDto(user));
    }

    private static UserDto MapToDto(Domain.Entities.User user) => new(
        user.Id,
        user.UserName,
        user.Email,
        user.FirstName,
        user.LastName,
        user.FullName,
        user.PhoneNumber,
        user.Status.ToString(),
        user.OrganizationId,
        user.DepartmentId,
        user.LastLoginAt.HasValue ? new DateTimeOffset(user.LastLoginAt.Value, TimeSpan.Zero) : null,
        new DateTimeOffset(user.CreatedAt, TimeSpan.Zero),
        user.UserRoles.Select(ur => ur.Role.Name).ToList());
}

// ─── GetUsersPaged ──────────────────────────────────────────
public sealed class GetUsersPagedHandler : IRequestHandler<GetUsersPagedQuery, Result<PagedResult<UserDto>>>
{
    private readonly IamDbContext _db;

    public GetUsersPagedHandler(IamDbContext db) => _db = db;

    public async Task<Result<PagedResult<UserDto>>> Handle(GetUsersPagedQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLowerInvariant();
            query = query.Where(u =>
                u.UserName.ToLower().Contains(search) ||
                u.Email.ToLower().Contains(search) ||
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search));
        }

        // Status filter
        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<Domain.Entities.UserStatus>(request.Status, true, out var status))
        {
            query = query.Where(u => u.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = users.Select(u => new UserDto(
            u.Id,
            u.UserName,
            u.Email,
            u.FirstName,
            u.LastName,
            u.FullName,
            u.PhoneNumber,
            u.Status.ToString(),
            u.OrganizationId,
            u.DepartmentId,
            u.LastLoginAt.HasValue ? new DateTimeOffset(u.LastLoginAt.Value, TimeSpan.Zero) : null,
            new DateTimeOffset(u.CreatedAt, TimeSpan.Zero),
            u.UserRoles.Select(ur => ur.Role.Name).ToList()))
            .ToList();

        return Result<PagedResult<UserDto>>.Success(
            new PagedResult<UserDto> { Items = items, TotalCount = totalCount, PageNumber = request.Page, PageSize = request.PageSize });
    }
}

// ─── GetRoles ───────────────────────────────────────────────
public sealed class GetRolesHandler : IRequestHandler<GetRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
    private readonly IamDbContext _db;

    public GetRolesHandler(IamDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var dtos = roles.Select(r => new RoleDto(
            r.Id,
            r.Name,
            r.DisplayName,
            r.Description,
            r.IsSystemRole,
            r.RolePermissions.Select(rp => rp.Permission.SystemName).ToList()))
            .ToList();

        return Result<IReadOnlyList<RoleDto>>.Success(dtos);
    }
}

// ─── GetOrganizationTree ────────────────────────────────────
public sealed class GetOrganizationTreeHandler : IRequestHandler<GetOrganizationTreeQuery, Result<IReadOnlyList<OrganizationDto>>>
{
    private readonly IamDbContext _db;

    public GetOrganizationTreeHandler(IamDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<OrganizationDto>>> Handle(GetOrganizationTreeQuery request, CancellationToken cancellationToken)
    {
        var allOrgs = await _db.Organizations
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);

        // Root organizasyonlardan başla ve ağacı kur
        var rootOrgs = allOrgs
            .Where(o => o.ParentId == null)
            .Select(o => BuildTree(o, allOrgs))
            .ToList();

        return Result<IReadOnlyList<OrganizationDto>>.Success(rootOrgs);
    }

    private static OrganizationDto BuildTree(Domain.Entities.Organization org, List<Domain.Entities.Organization> allOrgs)
    {
        var children = allOrgs
            .Where(o => o.ParentId == org.Id)
            .Select(o => BuildTree(o, allOrgs))
            .ToList();

        return new OrganizationDto(
            org.Id,
            org.Name,
            org.Code,
            org.ParentId,
            org.IsActive,
            children);
    }
}
