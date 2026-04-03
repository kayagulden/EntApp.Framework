using System.Text.RegularExpressions;
using EntApp.Modules.Notification.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace EntApp.Modules.Notification.Infrastructure.Providers;

/// <summary>
/// Basit mustache-style şablon renderer — {{variable}} yerine koyma.
/// Scriban güvenlik açıkları nedeniyle built-in çözüm kullanılıyor.
/// Gelecekte güvenli bir template engine'e upgrade edilebilir.
/// </summary>
public partial class SimpleTemplateRenderer : ITemplateRenderer
{
    private readonly ILogger<SimpleTemplateRenderer> _logger;

    public SimpleTemplateRenderer(ILogger<SimpleTemplateRenderer> logger) => _logger = logger;

    public Task<string> RenderAsync(string template, Dictionary<string, object> data, CancellationToken ct = default)
    {
        try
        {
            var result = TemplateRegex().Replace(template, match =>
            {
                var key = match.Groups[1].Value.Trim();
                return data.TryGetValue(key, out var value) ? value?.ToString() ?? "" : match.Value;
            });
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Template] Render hatası");
            return Task.FromResult(template);
        }
    }

    [GeneratedRegex(@"\{\{(\s*\w+\s*)\}\}")]
    private static partial Regex TemplateRegex();
}

/// <summary>SMTP e-posta provider (placeholder — gerçek SMTP gereksinimleri ile genişletilecek).</summary>
public class SmtpNotificationSender : INotificationSender
{
    public string Channel => "Email";
    private readonly ILogger<SmtpNotificationSender> _logger;

    public SmtpNotificationSender(ILogger<SmtpNotificationSender> logger) => _logger = logger;

    public Task<bool> SendAsync(string recipient, string subject, string body, CancellationToken ct = default)
    {
        // TODO: Gerçek SMTP implementasyonu (MailKit/SmtpClient)
        _logger.LogInformation("[SMTP] → {Recipient} | {Subject}", recipient, subject);
        return Task.FromResult(true);
    }
}

/// <summary>In-app bildirim provider — sadece DB'ye yazar, SignalR hub ile genişletilecek.</summary>
public class InAppNotificationSender : INotificationSender
{
    public string Channel => "InApp";
    private readonly ILogger<InAppNotificationSender> _logger;

    public InAppNotificationSender(ILogger<InAppNotificationSender> logger) => _logger = logger;

    public Task<bool> SendAsync(string recipient, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("[InApp] → {Recipient} | {Subject}", recipient, subject);
        return Task.FromResult(true);
    }
}
