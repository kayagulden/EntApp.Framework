using EntApp.Modules.Localization.Application.Commands;
using EntApp.Modules.Localization.Application.DTOs;
using EntApp.Modules.Localization.Application.Queries;
using EntApp.Modules.Localization.Domain.Entities;
using EntApp.Modules.Localization.Infrastructure.Persistence;
using EntApp.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EntApp.Modules.Localization.Infrastructure.Handlers;

// ─── CreateLanguage ─────────────────────────────────
public sealed class CreateLanguageHandler : IRequestHandler<CreateLanguageCommand, Result<Guid>>
{
    private readonly LocalizationDbContext _db;
    public CreateLanguageHandler(LocalizationDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateLanguageCommand req, CancellationToken ct)
    {
        var code = req.Code.Trim().ToLowerInvariant();
        if (await _db.Languages.AnyAsync(l => l.Code == code, ct))
            return Result<Guid>.Failure(Error.Conflict("Language.Exists", $"'{code}' dili zaten mevcut."));

        var lang = Language.Create(req.Code, req.Name, req.NativeName, req.IsDefault, req.DisplayOrder);

        if (req.IsDefault)
        {
            var existingDefault = await _db.Languages.FirstOrDefaultAsync(l => l.IsDefault, ct);
            existingDefault?.ClearDefault();
        }

        _db.Languages.Add(lang);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(lang.Id);
    }
}

// ─── SetDefaultLanguage ─────────────────────────────
public sealed class SetDefaultLanguageHandler : IRequestHandler<SetDefaultLanguageCommand, Result>
{
    private readonly LocalizationDbContext _db;
    public SetDefaultLanguageHandler(LocalizationDbContext db) => _db = db;

    public async Task<Result> Handle(SetDefaultLanguageCommand req, CancellationToken ct)
    {
        var lang = await _db.Languages.FindAsync([req.LanguageId], ct);
        if (lang is null) return Result.Failure(Error.NotFound("Language.NotFound", "Dil bulunamadı."));

        var existingDefault = await _db.Languages.FirstOrDefaultAsync(l => l.IsDefault, ct);
        existingDefault?.ClearDefault();

        lang.SetAsDefault();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── ToggleLanguage ─────────────────────────────────
public sealed class ToggleLanguageHandler : IRequestHandler<ToggleLanguageCommand, Result>
{
    private readonly LocalizationDbContext _db;
    public ToggleLanguageHandler(LocalizationDbContext db) => _db = db;

    public async Task<Result> Handle(ToggleLanguageCommand req, CancellationToken ct)
    {
        var lang = await _db.Languages.FindAsync([req.LanguageId], ct);
        if (lang is null) return Result.Failure(Error.NotFound("Language.NotFound", "Dil bulunamadı."));

        if (lang.IsActive) lang.Deactivate(); else lang.Activate();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── UpsertTranslation ──────────────────────────────
public sealed class UpsertTranslationHandler : IRequestHandler<UpsertTranslationCommand, Result<Guid>>
{
    private readonly LocalizationDbContext _db;
    public UpsertTranslationHandler(LocalizationDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(UpsertTranslationCommand req, CancellationToken ct)
    {
        var code = req.LanguageCode.Trim().ToLowerInvariant();
        var ns = req.Namespace.Trim();
        var key = req.Key.Trim();

        var existing = await _db.Translations.FirstOrDefaultAsync(
            t => t.LanguageCode == code && t.Namespace == ns && t.Key == key && t.TenantId == req.TenantId, ct);

        if (existing is not null)
        {
            existing.UpdateValue(req.Value, req.ModifiedBy);
            await _db.SaveChangesAsync(ct);
            return Result<Guid>.Success(existing.Id);
        }

        var entry = TranslationEntry.Create(code, ns, key, req.Value, req.TenantId);
        _db.Translations.Add(entry);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(entry.Id);
    }
}

// ─── BulkUpsertTranslations ─────────────────────────
public sealed class BulkUpsertHandler : IRequestHandler<BulkUpsertTranslationsCommand, Result<int>>
{
    private readonly LocalizationDbContext _db;
    public BulkUpsertHandler(LocalizationDbContext db) => _db = db;

    public async Task<Result<int>> Handle(BulkUpsertTranslationsCommand req, CancellationToken ct)
    {
        var count = 0;
        foreach (var dto in req.Translations)
        {
            var code = dto.LanguageCode.Trim().ToLowerInvariant();
            var ns = dto.Namespace.Trim();
            var key = dto.Key.Trim();

            var existing = await _db.Translations.FirstOrDefaultAsync(
                t => t.LanguageCode == code && t.Namespace == ns && t.Key == key && t.TenantId == req.TenantId, ct);

            if (existing is not null)
                existing.UpdateValue(dto.Value, req.ModifiedBy);
            else
                _db.Translations.Add(TranslationEntry.Create(code, ns, key, dto.Value, req.TenantId));

            count++;
        }

        await _db.SaveChangesAsync(ct);
        return Result<int>.Success(count);
    }
}

// ─── VerifyTranslation ──────────────────────────────
public sealed class VerifyTranslationHandler : IRequestHandler<VerifyTranslationCommand, Result>
{
    private readonly LocalizationDbContext _db;
    public VerifyTranslationHandler(LocalizationDbContext db) => _db = db;

    public async Task<Result> Handle(VerifyTranslationCommand req, CancellationToken ct)
    {
        var entry = await _db.Translations.FindAsync([req.TranslationId], ct);
        if (entry is null) return Result.Failure(Error.NotFound("Translation.NotFound", "Çeviri bulunamadı."));
        entry.Verify();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── DeleteTranslation ──────────────────────────────
public sealed class DeleteTranslationHandler : IRequestHandler<DeleteTranslationCommand, Result>
{
    private readonly LocalizationDbContext _db;
    public DeleteTranslationHandler(LocalizationDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteTranslationCommand req, CancellationToken ct)
    {
        var entry = await _db.Translations.FindAsync([req.TranslationId], ct);
        if (entry is null) return Result.Failure(Error.NotFound("Translation.NotFound", "Çeviri bulunamadı."));
        _db.Translations.Remove(entry);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── GetLanguages ───────────────────────────────────
public sealed class GetLanguagesHandler : IRequestHandler<GetLanguagesQuery, Result<IReadOnlyList<LanguageDto>>>
{
    private readonly LocalizationDbContext _db;
    public GetLanguagesHandler(LocalizationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<LanguageDto>>> Handle(GetLanguagesQuery req, CancellationToken ct)
    {
        var query = _db.Languages.AsNoTracking().AsQueryable();
        if (req.ActiveOnly) query = query.Where(l => l.IsActive);

        var items = await query.OrderBy(l => l.DisplayOrder).ThenBy(l => l.Name)
            .Select(l => new LanguageDto(l.Id, l.Code, l.Name, l.NativeName, l.IsDefault, l.IsActive, l.DisplayOrder))
            .ToListAsync(ct);

        return Result<IReadOnlyList<LanguageDto>>.Success(items);
    }
}

// ─── GetTranslations ────────────────────────────────
public sealed class GetTranslationsHandler : IRequestHandler<GetTranslationsQuery, Result<IReadOnlyList<TranslationDto>>>
{
    private readonly LocalizationDbContext _db;
    public GetTranslationsHandler(LocalizationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<TranslationDto>>> Handle(GetTranslationsQuery req, CancellationToken ct)
    {
        var code = req.LanguageCode.Trim().ToLowerInvariant();
        var query = _db.Translations.AsNoTracking().Where(t => t.LanguageCode == code);
        if (!string.IsNullOrWhiteSpace(req.Namespace)) query = query.Where(t => t.Namespace == req.Namespace);
        query = query.Where(t => t.TenantId == null || t.TenantId == req.TenantId);

        var items = await query.OrderBy(t => t.Namespace).ThenBy(t => t.Key)
            .Select(t => new TranslationDto(t.Id, t.LanguageCode, t.Namespace, t.Key, t.Value, t.IsVerified, t.FullKey, t.TenantId))
            .ToListAsync(ct);

        return Result<IReadOnlyList<TranslationDto>>.Success(items);
    }
}

// ─── GetTranslationsByKey ───────────────────────────
public sealed class GetTranslationsByKeyHandler : IRequestHandler<GetTranslationsByKeyQuery, Result<IReadOnlyList<TranslationDto>>>
{
    private readonly LocalizationDbContext _db;
    public GetTranslationsByKeyHandler(LocalizationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<TranslationDto>>> Handle(GetTranslationsByKeyQuery req, CancellationToken ct)
    {
        var query = _db.Translations.AsNoTracking().Where(t => t.Key == req.Key.Trim());
        if (!string.IsNullOrWhiteSpace(req.Namespace)) query = query.Where(t => t.Namespace == req.Namespace);

        var items = await query.OrderBy(t => t.LanguageCode)
            .Select(t => new TranslationDto(t.Id, t.LanguageCode, t.Namespace, t.Key, t.Value, t.IsVerified, t.FullKey, t.TenantId))
            .ToListAsync(ct);

        return Result<IReadOnlyList<TranslationDto>>.Success(items);
    }
}

// ─── GetTranslationMap ──────────────────────────────
public sealed class GetTranslationMapHandler : IRequestHandler<GetTranslationMapQuery, Result<Dictionary<string, string>>>
{
    private readonly LocalizationDbContext _db;
    public GetTranslationMapHandler(LocalizationDbContext db) => _db = db;

    public async Task<Result<Dictionary<string, string>>> Handle(GetTranslationMapQuery req, CancellationToken ct)
    {
        var code = req.LanguageCode.Trim().ToLowerInvariant();
        var query = _db.Translations.AsNoTracking().Where(t => t.LanguageCode == code);
        if (!string.IsNullOrWhiteSpace(req.Namespace)) query = query.Where(t => t.Namespace == req.Namespace);

        // Global çevirileri al
        var globals = await query.Where(t => t.TenantId == null)
            .ToDictionaryAsync(t => $"{t.Namespace}.{t.Key}", t => t.Value, ct);

        // Tenant override'ları al
        if (req.TenantId.HasValue)
        {
            var tenantOverrides = await query.Where(t => t.TenantId == req.TenantId)
                .ToDictionaryAsync(t => $"{t.Namespace}.{t.Key}", t => t.Value, ct);

            foreach (var kvp in tenantOverrides)
                globals[kvp.Key] = kvp.Value; // tenant override global
        }

        return Result<Dictionary<string, string>>.Success(globals);
    }
}
