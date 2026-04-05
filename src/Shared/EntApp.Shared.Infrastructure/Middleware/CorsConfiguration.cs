using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EntApp.Shared.Infrastructure.Middleware;

/// <summary>
/// CORS konfigürasyonu — appsettings.json'dan AllowedOrigins okur.
/// Development: localhost, Production: config-driven whitelist.
/// </summary>
public static class CorsConfiguration
{
    public const string PolicyName = "EntAppCors";

    /// <summary>CORS servislerini DI'a ekler.</summary>
    public static IServiceCollection AddEntAppCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Security:AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:3000"];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithExposedHeaders(
                        "Content-Disposition",  // Dosya indirme
                        "X-Pagination",         // Sayfalama bilgisi
                        "X-Request-Id"          // İstek takibi
                    );
            });
        });

        return services;
    }

    /// <summary>CORS middleware'ini kullanır.</summary>
    public static IApplicationBuilder UseEntAppCors(this IApplicationBuilder app)
    {
        return app.UseCors(PolicyName);
    }
}
