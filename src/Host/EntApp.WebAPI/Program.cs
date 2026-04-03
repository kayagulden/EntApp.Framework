using System.Reflection;
using EntApp.Shared.Contracts.Identity;
using EntApp.Shared.Infrastructure.Auth;
using EntApp.Shared.Infrastructure.Health;
using EntApp.Shared.Infrastructure.Middleware;
using EntApp.Shared.Infrastructure.Modules;
using EntApp.Shared.Infrastructure.RealTime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

// ═══════════════════════════════════════════════════════════════
//  EntApp.Framework — Composition Root
//  Modular Monolith Walking Skeleton
// ═══════════════════════════════════════════════════════════════

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("EntApp.WebAPI starting up...");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ──────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "EntApp.WebAPI"));

    // ── HttpContextAccessor ─────────────────────────────────
    builder.Services.AddHttpContextAccessor();

    // ── Authentication (Keycloak JWT) ───────────────────────
    builder.Services.AddKeycloakAuthentication(builder.Configuration);

    // ── Authorization (Permission-based RBAC) ───────────────
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

    // ── Current User & Tenant ───────────────────────────────
    builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
    builder.Services.AddScoped<ICurrentTenant, HttpContextCurrentTenant>();

    // ── SignalR ──────────────────────────────────────────────
    builder.Services.AddSignalR();
    builder.Services.AddSingleton<IUserConnectionTracker, InMemoryUserConnectionTracker>();
    builder.Services.AddScoped<IEntityChangeNotifier, EntityChangeNotifier>();

    // ── Health Checks ────────────────────────────────────────
    builder.Services
        .AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing."),
            name: "postgresql",
            tags: ["db", "ready"])
        .AddCheck<ModuleHealthCheckAdapter>("modules", tags: ["modules", "ready"]);

    // ── Swagger ──────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "EntApp API",
            Version = "v1",
            Description = "EntApp Modular Monolith Framework API"
        });

        // JWT Auth in Swagger
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter your JWT token"
        });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // ── Rate Limiting ────────────────────────────────────────
    builder.Services.AddRateLimiter(RateLimitingConfiguration.Configure);

    // ── Controllers ──────────────────────────────────────────
    builder.Services.AddControllers();

    // ── Module Auto-Discovery ────────────────────────────────
    // Yeni modül assembly'leri buraya eklenir
    builder.Services.AddModules(
        builder.Configuration,
        typeof(EntApp.Shared.Infrastructure.Modules.ModuleRegistration).Assembly
        // typeof(IAMModuleInstaller).Assembly,    // Faz 5'te eklenecek
        // typeof(CMSModuleInstaller).Assembly,     // Faz 6'da eklenecek
    );

    // ═════════════════════════════════════════════════════════
    //  MIDDLEWARE PIPELINE
    // ═════════════════════════════════════════════════════════
    var app = builder.Build();

    // ── Log loaded modules ──────────────────────────────────
    app.LogLoadedModules();

    // ── Exception Handling (en dış katman) ───────────────────
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // ── Swagger (Dev only) ──────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "EntApp API v1");
            options.RoutePrefix = "swagger";
        });
    }

    // ── Request Logging ─────────────────────────────────────
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseSerilogRequestLogging();

    // ── Rate Limiting ───────────────────────────────────────
    app.UseRateLimiter();

    // ── Tenant Resolution ───────────────────────────────────
    app.UseMiddleware<TenantResolutionMiddleware>();

    // ── Auth ─────────────────────────────────────────────────
    app.UseAuthentication();
    app.UseAuthorization();

    // ── SignalR Hub ──────────────────────────────────────────
    app.MapHub<EntAppHub>("/hubs/entapp");

    // ── Health Checks ────────────────────────────────────────
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = WriteHealthCheckResponse
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteHealthCheckResponse
    });

    // ── Controllers ──────────────────────────────────────────
    app.MapControllers();

    // ── Root Endpoint ────────────────────────────────────────
    app.MapGet("/", () => Results.Ok(new
    {
        Application = "EntApp.Framework",
        Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
        Environment = app.Environment.EnvironmentName,
        Timestamp = DateTimeOffset.UtcNow
    }))
    .WithName("GetApiInfo")
    .WithDescription("Returns API information");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// ── Health Check JSON Response Writer ────────────────────────
static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var response = new
    {
        status = report.Status.ToString(),
        duration = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            duration = e.Value.Duration.TotalMilliseconds,
            description = e.Value.Description,
            data = e.Value.Data
        })
    };
    return context.Response.WriteAsJsonAsync(response);
}
