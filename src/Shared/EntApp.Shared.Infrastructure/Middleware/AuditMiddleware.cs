using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EntApp.Shared.Infrastructure.Middleware;

/// <summary>
/// Hassas işlem audit loglama middleware.
/// POST/PUT/PATCH/DELETE isteklerini loglar.
/// PII (Personally Identifiable Information) verilerini maskeler (KVKK/GDPR uyumu).
/// </summary>
public sealed partial class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Sadece state-changing metotları logla
        if (!IsAuditableMethod(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var userId = context.User?.FindFirst("sub")?.Value ?? "anonymous";
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        var ipAddress = MaskIpAddress(context.Connection.RemoteIpAddress?.ToString());

        _logger.LogInformation(
            "[AUDIT] User={UserId} Method={Method} Path={Path} IP={IpAddress}",
            MaskUserId(userId), method, path, ipAddress);

        await _next(context);

        _logger.LogInformation(
            "[AUDIT:COMPLETE] User={UserId} Method={Method} Path={Path} Status={StatusCode}",
            MaskUserId(userId), method, path, context.Response.StatusCode);
    }

    private static bool IsAuditableMethod(string method)
    {
        return method is "POST" or "PUT" or "PATCH" or "DELETE";
    }

    // ========== PII Maskeleme (KVKK/GDPR) ==========

    /// <summary>
    /// E-posta adresini maskeler: u***@domain.com
    /// </summary>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "***";
        }

        var atIndex = email.IndexOf('@', StringComparison.Ordinal);
        if (atIndex <= 0)
        {
            return "***";
        }

        return $"{email[0]}***{email[atIndex..]}";
    }

    /// <summary>
    /// Telefon numarasını maskeler: +90***4567
    /// </summary>
    public static string MaskPhoneNumber(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return "***";
        }

        var cleaned = PhoneCleanRegex().Replace(phone, "");
        if (cleaned.Length < 4)
        {
            return "***";
        }

        return $"{cleaned[..3]}***{cleaned[^4..]}";
    }

    /// <summary>
    /// TC Kimlik / ID numarasını maskeler: 123***890
    /// </summary>
    public static string MaskIdentityNumber(string? idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber) || idNumber.Length < 6)
        {
            return "***";
        }

        return $"{idNumber[..3]}***{idNumber[^3..]}";
    }

    /// <summary>
    /// Kullanıcı ID'sini kısmi maskeler (son 8 karakter gösterilir).
    /// </summary>
    private static string MaskUserId(string userId)
    {
        if (userId.Length <= 8)
        {
            return userId;
        }

        return $"***{userId[^8..]}";
    }

    /// <summary>
    /// IP adresini kısmi maskeler: 192.168.***
    /// </summary>
    private static string MaskIpAddress(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return "***";
        }

        var lastDot = ip.LastIndexOf('.');
        if (lastDot < 0)
        {
            return "***";
        }

        return $"{ip[..lastDot]}.***";
    }

    [GeneratedRegex(@"[\s\-\(\)]")]
    private static partial Regex PhoneCleanRegex();
}
