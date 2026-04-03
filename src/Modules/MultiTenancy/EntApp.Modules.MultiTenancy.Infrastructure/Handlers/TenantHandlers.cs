using EntApp.Modules.MultiTenancy.Application.Abstractions;
using EntApp.Modules.MultiTenancy.Application.Commands;
using EntApp.Modules.MultiTenancy.Application.DTOs;
using EntApp.Modules.MultiTenancy.Application.Queries;
using EntApp.Modules.MultiTenancy.Domain.Entities;
using EntApp.Modules.MultiTenancy.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.MultiTenancy.Infrastructure.Handlers;

// ─── CreateTenant ───────────────────────────────────
public sealed class CreateTenantHandler : IRequestHandler<CreateTenantCommand, Result<Guid>>
{
    private readonly TenantDbContext _db;
    private readonly IEnumerable<ITenantSeeder> _seeders;
    private readonly ILogger<CreateTenantHandler> _logger;

    public CreateTenantHandler(TenantDbContext db, IEnumerable<ITenantSeeder> seeders, ILogger<CreateTenantHandler> logger)
    { _db = db; _seeders = seeders; _logger = logger; }

    public async Task<Result<Guid>> Handle(CreateTenantCommand req, CancellationToken ct)
    {
        if (await _db.Tenants.AnyAsync(t => t.Identifier == req.Identifier.ToLowerInvariant(), ct))
            return Result<Guid>.Failure(Error.Conflict("Tenant.Exists", $"'{req.Identifier}' tanımlayıcıya sahip tenant zaten mevcut."));

        var tenant = Tenant.Create(req.Name, req.Identifier, req.AdminEmail, req.DisplayName, req.Description, req.Plan);
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

        // Tenant Bootstrapper — tüm modüllerin seed'ini çalıştır
        foreach (var seeder in _seeders.OrderBy(s => s.Order))
        {
            try
            {
                await seeder.SeedAsync(tenant.Id, ct);
                _logger.LogInformation("[TenantSeeder] {Seeder} seed tamamlandı: {TenantId}", seeder.GetType().Name, tenant.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TenantSeeder] {Seeder} seed hatası: {TenantId}", seeder.GetType().Name, tenant.Id);
            }
        }

        return Result<Guid>.Success(tenant.Id);
    }
}

// ─── UpdateTenant ───────────────────────────────────
public sealed class UpdateTenantHandler : IRequestHandler<UpdateTenantCommand, Result>
{
    private readonly TenantDbContext _db;
    public UpdateTenantHandler(TenantDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateTenantCommand req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FindAsync([req.TenantId], ct);
        if (tenant is null) return Result.Failure(Error.NotFound("Tenant.NotFound", "Tenant bulunamadı."));
        tenant.UpdateInfo(req.DisplayName, req.Description, req.LogoUrl);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── Activate ───────────────────────────────────────
public sealed class ActivateTenantHandler : IRequestHandler<ActivateTenantCommand, Result>
{
    private readonly TenantDbContext _db;
    public ActivateTenantHandler(TenantDbContext db) => _db = db;

    public async Task<Result> Handle(ActivateTenantCommand req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FindAsync([req.TenantId], ct);
        if (tenant is null) return Result.Failure(Error.NotFound("Tenant.NotFound", "Tenant bulunamadı."));
        tenant.Activate();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── Suspend ────────────────────────────────────────
public sealed class SuspendTenantHandler : IRequestHandler<SuspendTenantCommand, Result>
{
    private readonly TenantDbContext _db;
    public SuspendTenantHandler(TenantDbContext db) => _db = db;

    public async Task<Result> Handle(SuspendTenantCommand req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FindAsync([req.TenantId], ct);
        if (tenant is null) return Result.Failure(Error.NotFound("Tenant.NotFound", "Tenant bulunamadı."));
        tenant.Suspend(req.Reason);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── Deactivate ─────────────────────────────────────
public sealed class DeactivateTenantHandler : IRequestHandler<DeactivateTenantCommand, Result>
{
    private readonly TenantDbContext _db;
    public DeactivateTenantHandler(TenantDbContext db) => _db = db;

    public async Task<Result> Handle(DeactivateTenantCommand req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FindAsync([req.TenantId], ct);
        if (tenant is null) return Result.Failure(Error.NotFound("Tenant.NotFound", "Tenant bulunamadı."));
        tenant.Deactivate();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── ChangePlan ─────────────────────────────────────
public sealed class ChangePlanHandler : IRequestHandler<ChangePlanCommand, Result>
{
    private readonly TenantDbContext _db;
    public ChangePlanHandler(TenantDbContext db) => _db = db;

    public async Task<Result> Handle(ChangePlanCommand req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.FindAsync([req.TenantId], ct);
        if (tenant is null) return Result.Failure(Error.NotFound("Tenant.NotFound", "Tenant bulunamadı."));
        tenant.ChangePlan(req.Plan);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── SetSubdomain ───────────────────────────────────
public sealed class SetSubdomainHandler : IRequestHandler<SetSubdomainCommand, Result>
{
    private readonly TenantDbContext _db;
    public SetSubdomainHandler(TenantDbContext db) => _db = db;

    public async Task<Result> Handle(SetSubdomainCommand req, CancellationToken ct)
    {
        if (await _db.Tenants.AnyAsync(t => t.Subdomain == req.Subdomain.ToLowerInvariant() && t.Id != req.TenantId, ct))
            return Result.Failure(Error.Conflict("Tenant.SubdomainTaken", "Bu subdomain zaten kullanılıyor."));

        var tenant = await _db.Tenants.FindAsync([req.TenantId], ct);
        if (tenant is null) return Result.Failure(Error.NotFound("Tenant.NotFound", "Tenant bulunamadı."));
        tenant.SetSubdomain(req.Subdomain);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── UpsertSetting ──────────────────────────────────
public sealed class UpsertSettingHandler : IRequestHandler<UpsertTenantSettingCommand, Result>
{
    private readonly TenantDbContext _db;
    public UpsertSettingHandler(TenantDbContext db) => _db = db;

    public async Task<Result> Handle(UpsertTenantSettingCommand req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.Include(t => t.Settings).FirstOrDefaultAsync(t => t.Id == req.TenantId, ct);
        if (tenant is null) return Result.Failure(Error.NotFound("Tenant.NotFound", "Tenant bulunamadı."));
        tenant.AddSetting(req.Key, req.Value);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── GetTenants ─────────────────────────────────────
public sealed class GetTenantsHandler : IRequestHandler<GetTenantsQuery, Result<PagedResult<TenantSummaryDto>>>
{
    private readonly TenantDbContext _db;
    public GetTenantsHandler(TenantDbContext db) => _db = db;

    public async Task<Result<PagedResult<TenantSummaryDto>>> Handle(GetTenantsQuery req, CancellationToken ct)
    {
        var query = _db.Tenants.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(req.Status) && Enum.TryParse<TenantStatus>(req.Status, true, out var status))
            query = query.Where(t => t.Status == status);
        if (!string.IsNullOrWhiteSpace(req.Plan))
            query = query.Where(t => t.Plan == req.Plan);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.Name)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .Select(t => new TenantSummaryDto(t.Id, t.Name, t.Identifier, t.Status.ToString(), t.Plan, t.AdminEmail))
            .ToListAsync(ct);

        return Result<PagedResult<TenantSummaryDto>>.Success(new PagedResult<TenantSummaryDto>(items, total, req.Page, req.PageSize));
    }
}

// ─── GetTenantById ──────────────────────────────────
public sealed class GetTenantByIdHandler : IRequestHandler<GetTenantByIdQuery, Result<TenantDto>>
{
    private readonly TenantDbContext _db;
    public GetTenantByIdHandler(TenantDbContext db) => _db = db;

    public async Task<Result<TenantDto>> Handle(GetTenantByIdQuery req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.AsNoTracking().Include(t => t.Settings).FirstOrDefaultAsync(t => t.Id == req.TenantId, ct);
        if (tenant is null) return Result<TenantDto>.Failure(Error.NotFound("Tenant.NotFound", "Tenant bulunamadı."));
        return Result<TenantDto>.Success(MapToDto(tenant));
    }
}

// ─── GetTenantByIdentifier ──────────────────────────
public sealed class GetTenantByIdentifierHandler : IRequestHandler<GetTenantByIdentifierQuery, Result<TenantDto>>
{
    private readonly TenantDbContext _db;
    public GetTenantByIdentifierHandler(TenantDbContext db) => _db = db;

    public async Task<Result<TenantDto>> Handle(GetTenantByIdentifierQuery req, CancellationToken ct)
    {
        var tenant = await _db.Tenants.AsNoTracking().Include(t => t.Settings)
            .FirstOrDefaultAsync(t => t.Identifier == req.Identifier.ToLowerInvariant(), ct);
        if (tenant is null) return Result<TenantDto>.Failure(Error.NotFound("Tenant.NotFound", "Tenant bulunamadı."));
        return Result<TenantDto>.Success(MapToDto(tenant));
    }
}

// ─── Helper ─────────────────────────────────────────
file static class Mapper
{
    // Intentionally empty
}

static file TenantDto MapToDto(Tenant t) => new(
    t.Id, t.Name, t.Identifier, t.DisplayName, t.Description, t.Subdomain,
    t.Status.ToString(), t.Plan, t.AdminEmail, t.LogoUrl,
    t.ActivatedAt, t.SuspendedAt,
    t.Settings.Select(s => new TenantSettingDto(s.Id, s.Key, s.Value)).ToList());
