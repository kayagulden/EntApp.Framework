using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace EntApp.Shared.Infrastructure.Middleware;

/// <summary>
/// OWASP önerilen güvenlik header'larını tüm response'lara ekler.
/// CSP, HSTS, X-Frame-Options vb. header'lar tarayıcı tarafında güvenlik sağlar.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _enableHsts;

    public SecurityHeadersMiddleware(RequestDelegate next, bool enableHsts = false)
    {
        _next = next;
        _enableHsts = enableHsts;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // ── OWASP Recommended Headers ────────────────────────────

        // XSS koruması — modern tarayıcılar CSP kullanır, bu legacy header
        headers["X-XSS-Protection"] = "0";

        // MIME sniffing engelleme
        headers["X-Content-Type-Options"] = "nosniff";

        // Clickjacking koruması — sayfanın iframe içinde yüklenmesini engeller
        headers["X-Frame-Options"] = "DENY";

        // Referrer bilgisi kısıtlama
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // İzin politikası — kamera, mikrofon, geolocation vb. kısıtlama
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

        // Content-Security-Policy — XSS ve injection saldırılarına karşı
        // Swagger UI için 'unsafe-inline' gerekli (development)
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: blob:; " +
            "font-src 'self' data:; " +
            "connect-src 'self' ws: wss:; " +
            "frame-ancestors 'none';";

        // HSTS — production'da HTTPS zorlaması
        if (_enableHsts)
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        }

        // Cache kontrolü — hassas veri içeren sayfalarda cache engelleme
        if (!context.Request.Path.StartsWithSegments("/swagger") &&
            !context.Request.Path.StartsWithSegments("/_next"))
        {
            headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            headers["Pragma"] = "no-cache";
        }

        // Server bilgisini gizle
        headers.Remove("Server");
        headers.Remove("X-Powered-By");

        await _next(context);
    }
}

/// <summary>SecurityHeadersMiddleware extension method.</summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>OWASP güvenlik header'larını ekler.</summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, bool enableHsts = false)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>(enableHsts);
    }
}
