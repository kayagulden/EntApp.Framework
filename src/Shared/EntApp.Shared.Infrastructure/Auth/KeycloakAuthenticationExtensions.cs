using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EntApp.Shared.Infrastructure.Auth;

/// <summary>
/// Keycloak JWT authentication konfigürasyonu.
/// appsettings.json'dan Keycloak ayarlarını okur.
/// </summary>
public static class KeycloakAuthenticationExtensions
{
    /// <summary>
    /// Keycloak JWT Bearer authentication ekler.
    /// </summary>
    /// <example>
    /// appsettings.json:
    /// <code>
    /// {
    ///   "Keycloak": {
    ///     "Authority": "http://localhost:8080/realms/entapp",
    ///     "Audience": "entapp-api",
    ///     "RequireHttpsMetadata": false
    ///   }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var keycloakSection = configuration.GetSection("Keycloak");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = keycloakSection["Authority"];
            options.Audience = keycloakSection["Audience"];
            options.RequireHttpsMetadata = keycloakSection.GetValue("RequireHttpsMetadata", true);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                // Keycloak role claim mapping
                RoleClaimType = "realm_role",
                NameClaimType = "preferred_username"
            };

            // Keycloak realm_access → roles mapping
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    MapKeycloakRoleClaims(context);
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Keycloak'un realm_access.roles JSON claim'ini flat role claim'lere dönüştürür.
    /// </summary>
    private static void MapKeycloakRoleClaims(TokenValidatedContext context)
    {
        if (context.Principal?.Identity is not System.Security.Claims.ClaimsIdentity identity)
        {
            return;
        }

        // Keycloak realm_access claim'ini parse et
        var realmAccess = context.Principal.FindFirst("realm_access")?.Value;
        if (realmAccess is null)
        {
            return;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(realmAccess);
            if (doc.RootElement.TryGetProperty("roles", out var roles))
            {
                foreach (var role in roles.EnumerateArray())
                {
                    var roleValue = role.GetString();
                    if (roleValue is not null)
                    {
                        identity.AddClaim(new System.Security.Claims.Claim("realm_role", roleValue));
                    }
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // realm_access claim parse edilemezse sessizce devam et
        }
    }
}
