using EntApp.Modules.Notification.Application.Abstractions;
using EntApp.Modules.Notification.Application.Commands;
using EntApp.Modules.Notification.Application.DTOs;
using EntApp.Modules.Notification.Application.Queries;
using EntApp.Modules.Notification.Domain.Entities;
using EntApp.Modules.Notification.Infrastructure.Persistence;
using EntApp.Shared.Contracts.Common;
using EntApp.Shared.Kernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.Notification.Infrastructure.Handlers;

// ─── CreateTemplate ─────────────────────────────────
public sealed class CreateTemplateHandler : IRequestHandler<CreateTemplateCommand, Result<Guid>>
{
    private readonly NotificationDbContext _db;
    public CreateTemplateHandler(NotificationDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateTemplateCommand req, CancellationToken ct)
    {
        if (!Enum.TryParse<NotificationChannel>(req.Channel, true, out var channel))
            return Result<Guid>.Failure(Error.Validation("Template.InvalidChannel", "Geçersiz kanal."));

        if (await _db.Templates.AnyAsync(t => t.Code == req.Code && t.Channel == channel && t.TenantId == req.TenantId, ct))
            return Result<Guid>.Failure(Error.Conflict("Template.Exists", "Bu şablon zaten mevcut."));

        var template = NotificationTemplate.Create(req.Code, req.Name, channel, req.Subject, req.Body, req.Description, req.Language, req.TenantId);
        _db.Templates.Add(template);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(template.Id);
    }
}

// ─── UpdateTemplate ─────────────────────────────────
public sealed class UpdateTemplateHandler : IRequestHandler<UpdateTemplateCommand, Result>
{
    private readonly NotificationDbContext _db;
    public UpdateTemplateHandler(NotificationDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateTemplateCommand req, CancellationToken ct)
    {
        var template = await _db.Templates.FindAsync([req.TemplateId], ct);
        if (template is null)
            return Result.Failure(Error.NotFound("Template.NotFound", "Şablon bulunamadı."));

        template.UpdateContent(req.Subject, req.Body);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── SendNotification ───────────────────────────────
public sealed class SendNotificationHandler : IRequestHandler<SendNotificationCommand, Result<Guid>>
{
    private readonly NotificationDbContext _db;
    private readonly IEnumerable<INotificationSender> _senders;
    private readonly ITemplateRenderer _renderer;
    private readonly ILogger<SendNotificationHandler> _logger;

    public SendNotificationHandler(NotificationDbContext db, IEnumerable<INotificationSender> senders,
        ITemplateRenderer renderer, ILogger<SendNotificationHandler> logger)
    { _db = db; _senders = senders; _renderer = renderer; _logger = logger; }

    public async Task<Result<Guid>> Handle(SendNotificationCommand req, CancellationToken ct)
    {
        if (!Enum.TryParse<NotificationChannel>(req.Channel, true, out var channel))
            return Result<Guid>.Failure(Error.Validation("Send.InvalidChannel", "Geçersiz kanal."));

        string subject = req.Subject ?? "";
        string body = req.Body ?? "";

        // Şablon varsa render et
        if (!string.IsNullOrWhiteSpace(req.TemplateCode))
        {
            var template = await _db.Templates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Code == req.TemplateCode && t.Channel == channel && t.IsActive, ct);

            if (template is null)
                return Result<Guid>.Failure(Error.NotFound("Template.NotFound", $"'{req.TemplateCode}' şablonu bulunamadı."));

            var data = req.TemplateData ?? new Dictionary<string, object>();
            subject = await _renderer.RenderAsync(template.Subject, data, ct);
            body = await _renderer.RenderAsync(template.Body, data, ct);
        }

        // Log kayıt oluştur
        var log = NotificationLog.Create(req.UserId, req.Recipient, channel, subject, body, req.TemplateCode, req.TenantId);

        // Provider ile gönder
        var sender = _senders.FirstOrDefault(s => s.Channel.Equals(req.Channel, StringComparison.OrdinalIgnoreCase));
        if (sender is not null)
        {
            try
            {
                var sent = await sender.SendAsync(req.Recipient, subject, body, ct);
                if (sent) log.MarkSent();
                else log.MarkFailed("Gönderim başarısız.");
            }
            catch (Exception ex)
            {
                log.MarkFailed(ex.Message);
                _logger.LogError(ex, "[Notification] Gönderim hatası: {Channel} → {Recipient}", req.Channel, req.Recipient);
            }
        }
        else
        {
            log.MarkSent(); // InApp — DB kaydı yeterli
        }

        _db.Logs.Add(log);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(log.Id);
    }
}

// ─── MarkNotificationRead ───────────────────────────
public sealed class MarkReadHandler : IRequestHandler<MarkNotificationReadCommand, Result>
{
    private readonly NotificationDbContext _db;
    public MarkReadHandler(NotificationDbContext db) => _db = db;

    public async Task<Result> Handle(MarkNotificationReadCommand req, CancellationToken ct)
    {
        var log = await _db.Logs.FindAsync([req.NotificationId], ct);
        if (log is null) return Result.Failure(Error.NotFound("Log.NotFound", "Bildirim bulunamadı."));
        log.MarkRead();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── UpdatePreference ───────────────────────────────
public sealed class UpdatePreferenceHandler : IRequestHandler<UpdatePreferenceCommand, Result>
{
    private readonly NotificationDbContext _db;
    public UpdatePreferenceHandler(NotificationDbContext db) => _db = db;

    public async Task<Result> Handle(UpdatePreferenceCommand req, CancellationToken ct)
    {
        if (!Enum.TryParse<NotificationChannel>(req.Channel, true, out var channel))
            return Result.Failure(Error.Validation("Pref.InvalidChannel", "Geçersiz kanal."));

        var pref = await _db.Preferences
            .FirstOrDefaultAsync(p => p.UserId == req.UserId && p.Channel == channel && p.TenantId == req.TenantId, ct);

        if (pref is null)
        {
            pref = UserNotificationPreference.Create(req.UserId, channel, req.IsEnabled, req.TenantId);
            _db.Preferences.Add(pref);
        }
        else
        {
            if (req.IsEnabled) pref.Enable(); else pref.Disable();
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── GetTemplates ───────────────────────────────────
public sealed class GetTemplatesHandler : IRequestHandler<GetTemplatesQuery, Result<IReadOnlyList<NotificationTemplateDto>>>
{
    private readonly NotificationDbContext _db;
    public GetTemplatesHandler(NotificationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<NotificationTemplateDto>>> Handle(GetTemplatesQuery req, CancellationToken ct)
    {
        var query = _db.Templates.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(req.Channel) && Enum.TryParse<NotificationChannel>(req.Channel, true, out var ch))
            query = query.Where(t => t.Channel == ch);
        query = query.Where(t => t.TenantId == null || t.TenantId == req.TenantId);

        var items = await query.OrderBy(t => t.Code)
            .Select(t => new NotificationTemplateDto(t.Id, t.Code, t.Name, t.Description,
                t.Channel.ToString(), t.Subject, t.Body, t.Language, t.IsActive, t.TenantId))
            .ToListAsync(ct);

        return Result<IReadOnlyList<NotificationTemplateDto>>.Success(items);
    }
}

// ─── GetNotificationHistory ─────────────────────────
public sealed class GetHistoryHandler : IRequestHandler<GetNotificationHistoryQuery, Result<PagedResult<NotificationLogDto>>>
{
    private readonly NotificationDbContext _db;
    public GetHistoryHandler(NotificationDbContext db) => _db = db;

    public async Task<Result<PagedResult<NotificationLogDto>>> Handle(GetNotificationHistoryQuery req, CancellationToken ct)
    {
        var query = _db.Logs.AsNoTracking().AsQueryable();
        if (req.UserId.HasValue) query = query.Where(l => l.UserId == req.UserId);
        if (!string.IsNullOrWhiteSpace(req.Channel) && Enum.TryParse<NotificationChannel>(req.Channel, true, out var ch))
            query = query.Where(l => l.Channel == ch);
        if (!string.IsNullOrWhiteSpace(req.Status) && Enum.TryParse<NotificationStatus>(req.Status, true, out var st))
            query = query.Where(l => l.Status == st);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(l => l.SentAt)
            .Skip((req.Page - 1) * req.PageSize).Take(req.PageSize)
            .Select(l => new NotificationLogDto(l.Id, l.UserId, l.Recipient, l.Channel.ToString(),
                l.TemplateCode, l.Subject, l.Body, l.Status.ToString(), l.ErrorMessage, l.SentAt, l.ReadAt))
            .ToListAsync(ct);

        return Result<PagedResult<NotificationLogDto>>.Success(new PagedResult<NotificationLogDto> { Items = items, TotalCount = total, PageNumber = req.Page, PageSize = req.PageSize });
    }
}

// ─── GetUserPreferences ─────────────────────────────
public sealed class GetPreferencesHandler : IRequestHandler<GetUserPreferencesQuery, Result<IReadOnlyList<UserPreferenceDto>>>
{
    private readonly NotificationDbContext _db;
    public GetPreferencesHandler(NotificationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<UserPreferenceDto>>> Handle(GetUserPreferencesQuery req, CancellationToken ct)
    {
        var items = await _db.Preferences.AsNoTracking()
            .Where(p => p.UserId == req.UserId && (p.TenantId == null || p.TenantId == req.TenantId))
            .Select(p => new UserPreferenceDto(p.Id, p.UserId, p.Channel.ToString(), p.IsEnabled))
            .ToListAsync(ct);
        return Result<IReadOnlyList<UserPreferenceDto>>.Success(items);
    }
}

// ─── GetUnreadCount ─────────────────────────────────
public sealed class GetUnreadCountHandler : IRequestHandler<GetUnreadCountQuery, Result<int>>
{
    private readonly NotificationDbContext _db;
    public GetUnreadCountHandler(NotificationDbContext db) => _db = db;

    public async Task<Result<int>> Handle(GetUnreadCountQuery req, CancellationToken ct)
    {
        var count = await _db.Logs.CountAsync(
            l => l.UserId == req.UserId && l.Status != NotificationStatus.Read, ct);
        return Result<int>.Success(count);
    }
}
