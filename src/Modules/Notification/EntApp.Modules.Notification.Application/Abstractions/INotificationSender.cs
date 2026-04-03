namespace EntApp.Modules.Notification.Application.Abstractions;

/// <summary>Bildirim gönderim provider soyutlaması.</summary>
public interface INotificationSender
{
    string Channel { get; }
    Task<bool> SendAsync(string recipient, string subject, string body, CancellationToken ct = default);
}

/// <summary>Şablon render engine soyutlaması.</summary>
public interface ITemplateRenderer
{
    Task<string> RenderAsync(string template, Dictionary<string, object> data, CancellationToken ct = default);
}
