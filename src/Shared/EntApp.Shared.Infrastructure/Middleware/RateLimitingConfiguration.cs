using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Shared.Infrastructure.Middleware;

/// <summary>
/// ASP.NET Core Rate Limiter konfigürasyonu.
/// Fixed window, sliding window ve token bucket policy'leri içerir.
/// </summary>
public static class RateLimitingConfiguration
{
    /// <summary>Genel API rate limit policy adı.</summary>
    public const string GlobalPolicy = "global";

    /// <summary>Authenticated kullanıcı policy adı.</summary>
    public const string AuthenticatedPolicy = "authenticated";

    /// <summary>
    /// Rate limiting servislerini DI container'a ekler.
    /// </summary>
    public static IServiceCollection AddRateLimitingPolicies(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            // 429 Too Many Requests response
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global policy — IP bazlı fixed window
            options.AddFixedWindowLimiter(GlobalPolicy, opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 10;
            });

            // Authenticated policy — UserId bazlı sliding window
            options.AddSlidingWindowLimiter(AuthenticatedPolicy, opt =>
            {
                opt.PermitLimit = 200;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 4;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 20;
            });

            // Global fallback — partition by IP
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: remoteIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1)
                    });
            });

            // OnRejected callback
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString("F0");
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://httpstatuses.com/429",
                    title = "Too Many Requests",
                    status = 429,
                    detail = "Rate limit aşıldı. Lütfen daha sonra tekrar deneyin."
                }, cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// Rate limiting middleware'ini pipeline'a ekler.
    /// </summary>
    public static IApplicationBuilder UseRateLimitingPolicies(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseRateLimiter();
    }
}
