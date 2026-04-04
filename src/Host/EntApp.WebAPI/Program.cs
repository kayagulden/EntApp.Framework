using System.Reflection;
using Asp.Versioning;
using EntApp.Shared.Contracts.Identity;
using EntApp.Shared.Infrastructure.Auth;
using EntApp.Shared.Infrastructure.DynamicCrud;
using EntApp.Shared.Infrastructure.Health;
using EntApp.Shared.Infrastructure.Middleware;
using EntApp.Shared.Infrastructure.Modules;
using EntApp.Shared.Infrastructure.Persistence;
using EntApp.Shared.Infrastructure.RealTime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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

    // ── OpenTelemetry Tracing ────────────────────────────────
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(
                serviceName: "EntApp.WebAPI",
                serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.Filter = httpContext =>
                    !httpContext.Request.Path.StartsWithSegments("/health");
            })
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(
                    builder.Configuration["OpenTelemetry:OtlpEndpoint"]
                    ?? "http://localhost:4317");
            }));

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
    builder.Services.AddRateLimitingPolicies();

    // ── API Versioning ───────────────────────────────────────
    builder.Services
        .AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"));
        });

    // ── Controllers ──────────────────────────────────────────
    builder.Services.AddControllers();

    // ── Module Auto-Discovery ────────────────────────────────
    // Yeni modül assembly'leri buraya eklenir
    builder.Services.AddModules(
        builder.Configuration,
        typeof(EntApp.Shared.Infrastructure.Modules.ModuleRegistration).Assembly,
        typeof(EntApp.Modules.IAM.Infrastructure.IamModuleInstaller).Assembly,
        typeof(EntApp.Modules.Configuration.Infrastructure.ConfigModuleInstaller).Assembly
    );

    // ── Dynamic CRUD Engine ──────────────────────────────────
    // [DynamicEntity] attribute'ü olan entity'ler için otomatik metadata + CRUD
    builder.Services.AddDynamicCrud(
        typeof(EntApp.Modules.IAM.Infrastructure.IamModuleInstaller).Assembly,
        typeof(EntApp.Modules.Configuration.Domain.Entities.Country).Assembly
    );

    // Dynamic entity → DbContext eşleştirmesi
    builder.Services.AddDynamicDbContext<
        EntApp.Modules.Configuration.Infrastructure.Persistence.ConfigDbContext>(
        typeof(EntApp.Modules.Configuration.Domain.Entities.Country),
        typeof(EntApp.Modules.Configuration.Domain.Entities.City),
        typeof(EntApp.Modules.Configuration.Domain.Entities.Currency)
    );

    // ═════════════════════════════════════════════════════════
    //  MIDDLEWARE PIPELINE
    // ═════════════════════════════════════════════════════════
    var app = builder.Build();

    // ── Log loaded modules ──────────────────────────────────
    app.LogLoadedModules();

    // ── Database Migration (startup'ta) ─────────────────────
    // Modül DbContext'leri eklendikçe migration buraya eklenir:
    // await app.Services.MigrateDatabaseAsync<IAMDbContext>();

    // ── Seed Data (dev ortamda) ─────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        await app.Services.SeedDatabaseAsync();
    }

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

    // ── Dynamic CRUD Endpoints ───────────────────────────────
    app.MapDynamicCrudEndpoints();

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
