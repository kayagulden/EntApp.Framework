using EntApp.Modules.Configuration.Application.Commands;
using EntApp.Modules.Configuration.Application.DTOs;
using EntApp.Modules.Configuration.Application.Queries;
using EntApp.Modules.Configuration.Domain.Entities;
using EntApp.Modules.Configuration.Infrastructure.Persistence;
using EntApp.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.Configuration.Infrastructure.Handlers;

// ─── UpsertAppSetting ───────────────────────────────
public sealed class UpsertAppSettingHandler : IRequestHandler<UpsertAppSettingCommand, Result<Guid>>
{
    private readonly ConfigDbContext _db;
    private readonly ILogger<UpsertAppSettingHandler> _logger;

    public UpsertAppSettingHandler(ConfigDbContext db, ILogger<UpsertAppSettingHandler> logger) { _db = db; _logger = logger; }

    public async Task<Result<Guid>> Handle(UpsertAppSettingCommand req, CancellationToken ct)
    {
        if (!Enum.TryParse<SettingValueType>(req.ValueType, true, out var valueType))
            return Result<Guid>.Failure(Error.Validation("Setting.InvalidType", "Geçersiz değer tipi."));

        var existing = await _db.AppSettings
            .FirstOrDefaultAsync(s => s.Key == req.Key && s.TenantId == req.TenantId, ct);

        if (existing is not null)
        {
            existing.UpdateValue(req.Value);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("[Config] Setting updated: {Key}", req.Key);
            return Result<Guid>.Success(existing.Id);
        }

        var setting = AppSetting.Create(req.Key, req.Value, valueType, req.Description, req.Group, req.TenantId, req.IsEncrypted);
        _db.AppSettings.Add(setting);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("[Config] Setting created: {Key}", req.Key);
        return Result<Guid>.Success(setting.Id);
    }
}

// ─── CreateFeatureFlag ──────────────────────────────
public sealed class CreateFeatureFlagHandler : IRequestHandler<CreateFeatureFlagCommand, Result<Guid>>
{
    private readonly ConfigDbContext _db;

    public CreateFeatureFlagHandler(ConfigDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateFeatureFlagCommand req, CancellationToken ct)
    {
        if (await _db.FeatureFlags.AnyAsync(f => f.Name == req.Name && f.TenantId == req.TenantId, ct))
            return Result<Guid>.Failure(Error.Conflict("Flag.NameExists", "Bu flag adı zaten mevcut."));

        var flag = FeatureFlag.Create(req.Name, req.DisplayName, req.Description, req.IsEnabled, req.TenantId);
        _db.FeatureFlags.Add(flag);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(flag.Id);
    }
}

// ─── ToggleFeatureFlag ──────────────────────────────
public sealed class ToggleFeatureFlagHandler : IRequestHandler<ToggleFeatureFlagCommand, Result>
{
    private readonly ConfigDbContext _db;
    private readonly ILogger<ToggleFeatureFlagHandler> _logger;

    public ToggleFeatureFlagHandler(ConfigDbContext db, ILogger<ToggleFeatureFlagHandler> logger) { _db = db; _logger = logger; }

    public async Task<Result> Handle(ToggleFeatureFlagCommand req, CancellationToken ct)
    {
        var flag = await _db.FeatureFlags.FindAsync([req.FlagId], ct);
        if (flag is null)
            return Result.Failure(Error.NotFound("Flag.NotFound", "Feature flag bulunamadı."));

        flag.Toggle();
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("[Config] Flag toggled: {Name} → {State}", flag.Name, flag.IsEnabled);
        return Result.Success();
    }
}

// ─── SetFeatureFlagSchedule ─────────────────────────
public sealed class SetFeatureFlagScheduleHandler : IRequestHandler<SetFeatureFlagScheduleCommand, Result>
{
    private readonly ConfigDbContext _db;

    public SetFeatureFlagScheduleHandler(ConfigDbContext db) => _db = db;

    public async Task<Result> Handle(SetFeatureFlagScheduleCommand req, CancellationToken ct)
    {
        var flag = await _db.FeatureFlags.FindAsync([req.FlagId], ct);
        if (flag is null)
            return Result.Failure(Error.NotFound("Flag.NotFound", "Feature flag bulunamadı."));

        flag.SetSchedule(req.EnabledFrom, req.EnabledUntil);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── GetAppSettings ─────────────────────────────────
public sealed class GetAppSettingsHandler : IRequestHandler<GetAppSettingsQuery, Result<IReadOnlyList<AppSettingDto>>>
{
    private readonly ConfigDbContext _db;

    public GetAppSettingsHandler(ConfigDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<AppSettingDto>>> Handle(GetAppSettingsQuery req, CancellationToken ct)
    {
        var query = _db.AppSettings.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Group))
            query = query.Where(s => s.Group == req.Group);

        // Global + tenant-specific fallback
        query = query.Where(s => s.TenantId == null || s.TenantId == req.TenantId);

        var items = await query.OrderBy(s => s.Group).ThenBy(s => s.Key)
            .Select(s => new AppSettingDto(
                s.Id, s.Key, s.IsEncrypted ? "***" : s.Value,
                s.ValueType.ToString(), s.Description, s.Group,
                s.TenantId, s.IsEncrypted, s.IsReadOnly))
            .ToListAsync(ct);

        return Result<IReadOnlyList<AppSettingDto>>.Success(items);
    }
}

// ─── GetAppSettingByKey ─────────────────────────────
public sealed class GetAppSettingByKeyHandler : IRequestHandler<GetAppSettingByKeyQuery, Result<AppSettingDto>>
{
    private readonly ConfigDbContext _db;

    public GetAppSettingByKeyHandler(ConfigDbContext db) => _db = db;

    public async Task<Result<AppSettingDto>> Handle(GetAppSettingByKeyQuery req, CancellationToken ct)
    {
        // Tenant-specific > global fallback
        var setting = await _db.AppSettings.AsNoTracking()
            .Where(s => s.Key == req.Key && s.TenantId == req.TenantId)
            .FirstOrDefaultAsync(ct);

        setting ??= await _db.AppSettings.AsNoTracking()
            .Where(s => s.Key == req.Key && s.TenantId == null)
            .FirstOrDefaultAsync(ct);

        if (setting is null)
            return Result<AppSettingDto>.Failure(Error.NotFound("Setting.NotFound", $"'{req.Key}' ayarı bulunamadı."));

        return Result<AppSettingDto>.Success(new AppSettingDto(
            setting.Id, setting.Key, setting.IsEncrypted ? "***" : setting.Value,
            setting.ValueType.ToString(), setting.Description, setting.Group,
            setting.TenantId, setting.IsEncrypted, setting.IsReadOnly));
    }
}

// ─── GetFeatureFlags ────────────────────────────────
public sealed class GetFeatureFlagsHandler : IRequestHandler<GetFeatureFlagsQuery, Result<IReadOnlyList<FeatureFlagDto>>>
{
    private readonly ConfigDbContext _db;

    public GetFeatureFlagsHandler(ConfigDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<FeatureFlagDto>>> Handle(GetFeatureFlagsQuery req, CancellationToken ct)
    {
        var query = _db.FeatureFlags.AsNoTracking()
            .Where(f => f.TenantId == null || f.TenantId == req.TenantId);

        var items = await query.OrderBy(f => f.Name)
            .Select(f => new FeatureFlagDto(
                f.Id, f.Name, f.DisplayName, f.Description,
                f.IsEnabled, f.IsEffectivelyEnabled(),
                f.TenantId, f.EnabledFrom, f.EnabledUntil, f.AllowedRoles))
            .ToListAsync(ct);

        return Result<IReadOnlyList<FeatureFlagDto>>.Success(items);
    }
}

// ─── IsFeatureEnabled ───────────────────────────────
public sealed class IsFeatureEnabledHandler : IRequestHandler<IsFeatureEnabledQuery, Result<bool>>
{
    private readonly ConfigDbContext _db;

    public IsFeatureEnabledHandler(ConfigDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(IsFeatureEnabledQuery req, CancellationToken ct)
    {
        // Tenant-specific > global fallback
        var flag = await _db.FeatureFlags.AsNoTracking()
            .Where(f => f.Name == req.FlagName && f.TenantId == req.TenantId)
            .FirstOrDefaultAsync(ct);

        flag ??= await _db.FeatureFlags.AsNoTracking()
            .Where(f => f.Name == req.FlagName && f.TenantId == null)
            .FirstOrDefaultAsync(ct);

        return Result<bool>.Success(flag?.IsEffectivelyEnabled() ?? false);
    }
}
