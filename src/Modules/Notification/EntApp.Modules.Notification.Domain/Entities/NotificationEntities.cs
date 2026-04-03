using EntApp.Shared.Kernel.Domain;

namespace EntApp.Modules.Notification.Domain.Entities;

public enum NotificationChannel
{
    Email,
    InApp,
    Sms,
    Push
}

/// <summary>
/// Bildirim şablonu — Scriban template engine ile dinamik içerik üretimi.
/// </summary>
public class NotificationTemplate : AuditableEntity<Guid>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? Language { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid? TenantId { get; private set; }

    private NotificationTemplate() { }

    public static NotificationTemplate Create(
        string code, string name, NotificationChannel channel,
        string subject, string body,
        string? description = null, string? language = null, Guid? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Şablon kodu boş olamaz.", nameof(code));
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Konu boş olamaz.", nameof(subject));

        return new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Name = name,
            Channel = channel,
            Subject = subject,
            Body = body,
            Description = description,
            Language = language ?? "tr",
            IsActive = true,
            TenantId = tenantId
        };
    }

    public void UpdateContent(string subject, string body)
    {
        Subject = subject;
        Body = body;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

/// <summary>
/// Gönderilmiş bildirim kaydı — geçmiş ve takip.
/// </summary>
public class NotificationLog : BaseEntity<Guid>
{
    public Guid? UserId { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public NotificationChannel Channel { get; private set; }
    public string? TemplateCode { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public Guid? TenantId { get; private set; }

    private NotificationLog() { }

    public static NotificationLog Create(
        Guid? userId, string recipient, NotificationChannel channel,
        string subject, string body, string? templateCode = null, Guid? tenantId = null)
    {
        return new NotificationLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Recipient = recipient,
            Channel = channel,
            Subject = subject,
            Body = body,
            TemplateCode = templateCode,
            Status = NotificationStatus.Pending,
            SentAt = DateTime.UtcNow,
            TenantId = tenantId
        };
    }

    public void MarkSent() => Status = NotificationStatus.Sent;
    public void MarkFailed(string error) { Status = NotificationStatus.Failed; ErrorMessage = error; }
    public void MarkRead() { Status = NotificationStatus.Read; ReadAt = DateTime.UtcNow; }
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed,
    Read
}

/// <summary>
/// Kullanıcı bildirim tercihleri — kanal bazlı opt-in/opt-out.
/// </summary>
public class UserNotificationPreference : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public bool IsEnabled { get; private set; } = true;
    public Guid? TenantId { get; private set; }

    private UserNotificationPreference() { }

    public static UserNotificationPreference Create(
        Guid userId, NotificationChannel channel, bool isEnabled = true, Guid? tenantId = null)
    {
        return new UserNotificationPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Channel = channel,
            IsEnabled = isEnabled,
            TenantId = tenantId
        };
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
    public void Toggle() => IsEnabled = !IsEnabled;
}
