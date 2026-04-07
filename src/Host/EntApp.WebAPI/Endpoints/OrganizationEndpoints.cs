using EntApp.Shared.Infrastructure.Persistence;
using EntApp.Shared.Kernel.Domain.Entities;
using EntApp.Shared.Kernel.Domain.Ids;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace EntApp.WebAPI.Endpoints;

/// <summary>Organization ve Department REST API endpoint'leri — org şeması CRUD.</summary>
public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        // ═══════════ Organizations ═══════════
        var orgs = app.MapGroup("/api/v1/org/organizations").WithTags("Organization - Organizations");

        orgs.MapGet("/tree", async (OrganizationDbContext db) =>
        {
            var allOrgs = await db.Organizations.AsNoTracking().OrderBy(o => o.Name).ToListAsync();
            var roots = allOrgs
                .Where(o => o.ParentId == null)
                .Select(o => BuildOrgTree(o, allOrgs))
                .ToList();
            return Results.Ok(roots);
        }).WithName("GetOrganizationTree_V2");

        orgs.MapPost("/", async (CreateOrganizationRequest req, OrganizationDbContext db) =>
        {
            if (await db.Organizations.AnyAsync(o => o.Code == req.Code.ToUpperInvariant()))
                return Results.Conflict(new { error = "Bu organizasyon kodu zaten kullanımda." });

            var parentId = req.ParentId.HasValue ? new OrganizationId(req.ParentId.Value) : (OrganizationId?)null;
            var org = Organization.Create(req.Name, req.Code, parentId);
            db.Organizations.Add(org);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/org/organizations/{org.Id.Value}", new { id = org.Id.Value });
        }).WithName("CreateOrganization_V2");

        // ═══════════ Departments ═══════════
        var depts = app.MapGroup("/api/v1/org/departments").WithTags("Organization - Departments");

        depts.MapGet("/", async (OrganizationDbContext db, bool? activeOnly) =>
        {
            var query = db.Departments.AsNoTracking().AsQueryable();
            if (activeOnly ?? true) query = query.Where(d => d.IsActive);
            var items = await query.OrderBy(d => d.Name).ToListAsync();
            return Results.Ok(items.Select(d => MapDeptDto(d)));
        }).WithName("ListDepartments");

        depts.MapGet("/{id:guid}", async (Guid id, OrganizationDbContext db) =>
        {
            var dept = await db.Departments
                .Include(d => d.SubDepartments)
                .FirstOrDefaultAsync(d => d.Id == new DepartmentId(id));
            return dept is null ? Results.NotFound() : Results.Ok(MapDeptDto(dept));
        }).WithName("GetDepartment");

        depts.MapPost("/", async (CreateDepartmentRequest req, OrganizationDbContext db) =>
        {
            var orgId = req.OrganizationId.HasValue ? new OrganizationId(req.OrganizationId.Value) : (OrganizationId?)null;
            var parentId = req.ParentDepartmentId.HasValue ? new DepartmentId(req.ParentDepartmentId.Value) : (DepartmentId?)null;

            var dept = Department.Create(req.Name, req.Code, req.Description, orgId,
                req.ManagerUserId, parentId, req.DefaultQueueId);
            db.Departments.Add(dept);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/org/departments/{dept.Id.Value}", new { id = dept.Id.Value });
        }).WithName("CreateDepartment");

        depts.MapPut("/{id:guid}", async (Guid id, UpdateDepartmentRequest req, OrganizationDbContext db) =>
        {
            var dept = await db.Departments.FindAsync(new DepartmentId(id));
            if (dept is null) return Results.NotFound();

            var orgId = req.OrganizationId.HasValue ? new OrganizationId(req.OrganizationId.Value) : (OrganizationId?)null;
            var parentId = req.ParentDepartmentId.HasValue ? new DepartmentId(req.ParentDepartmentId.Value) : (DepartmentId?)null;

            dept.Update(req.Name, req.Code, req.Description, req.ManagerUserId, parentId, orgId, req.DefaultQueueId);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithName("UpdateDepartment");

        return app;
    }

    private static object MapDeptDto(Department d) => new
    {
        id = d.Id.Value,
        name = d.Name,
        code = d.Code,
        description = d.Description,
        organizationId = d.OrganizationId?.Value,
        managerUserId = d.ManagerUserId,
        parentDepartmentId = d.ParentDepartmentId?.Value,
        defaultQueueId = d.DefaultQueueId,
        isActive = d.IsActive,
        subDepartments = d.SubDepartments?.Select(s => new
        {
            id = s.Id.Value, name = s.Name, code = s.Code, isActive = s.IsActive
        }).ToList()
    };

    private static object BuildOrgTree(Organization org, List<Organization> all) => new
    {
        id = org.Id.Value,
        name = org.Name,
        code = org.Code,
        parentId = org.ParentId?.Value,
        isActive = org.IsActive,
        children = all.Where(o => o.ParentId == org.Id).Select(o => BuildOrgTree(o, all)).ToList()
    };
}

// ── Request DTOs ──────────────────────────────────────────────
public sealed record CreateOrganizationRequest(string Name, string Code, Guid? ParentId = null);
public sealed record CreateDepartmentRequest(string Name, string Code, string? Description = null,
    Guid? OrganizationId = null, Guid? ManagerUserId = null, Guid? ParentDepartmentId = null, Guid? DefaultQueueId = null);
public sealed record UpdateDepartmentRequest(string Name, string Code, string? Description = null,
    Guid? OrganizationId = null, Guid? ManagerUserId = null, Guid? ParentDepartmentId = null, Guid? DefaultQueueId = null);
