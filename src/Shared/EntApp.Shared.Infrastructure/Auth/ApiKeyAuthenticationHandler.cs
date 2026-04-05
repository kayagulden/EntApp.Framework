using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EntApp.Shared.Infrastructure.Auth;

/// <summary>
/// API Key authentication handler — dış sistem entegrasyonları için.
/// X-API-Key header ile gelen istekleri doğrular.
/// JWT (Bearer) ile birlikte çalışır (dual scheme).
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly IConfiguration _configuration;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration) : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Header yoksa bu scheme'i atla (Bearer denesin)
        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var providedKey = apiKeyHeader.ToString();
        if (string.IsNullOrEmpty(providedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API Key is empty."));
        }

        // Config'den key listesini oku
        var apiKeys = _configuration.GetSection("Security:ApiKeys")
            .Get<List<ApiKeyConfig>>() ?? [];

        var matchedKey = apiKeys.FirstOrDefault(k =>
            string.Equals(k.Key, providedKey, StringComparison.Ordinal));

        if (matchedKey is null)
        {
            Logger.LogWarning("[Security] Invalid API key attempt: {KeyPrefix}***",
                providedKey.Length > 8 ? providedKey[..8] : "***");
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key."));
        }

        // Claims oluştur
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, matchedKey.Name),
            new(ClaimTypes.NameIdentifier, $"apikey:{matchedKey.Name}"),
            new("auth_method", "api_key")
        };

        // Roller ekle
        foreach (var role in matchedKey.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogInformation("[Security] API key authenticated: {ClientName}", matchedKey.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>API Key authentication scheme options.</summary>
public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions;

/// <summary>API Key konfigürasyonu (appsettings.json).</summary>
public sealed class ApiKeyConfig
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}

/// <summary>API Key authentication DI extensions.</summary>
public static class ApiKeyAuthenticationExtensions
{
    public const string SchemeName = "ApiKey";

    /// <summary>API Key authentication scheme'ini ekler (Bearer ile birlikte çalışır).</summary>
    public static IServiceCollection AddApiKeyAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication()
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(SchemeName, null);

        return services;
    }
}
