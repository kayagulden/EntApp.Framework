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
using EntApp.Shared.Contracts.Messaging;
using EntApp.Shared.Infrastructure.Messaging;
using EntApp.Modules.AI.Infrastructure.Endpoints;
using EntApp.Modules.Workflow.Infrastructure.Endpoints;
using EntApp.Modules.CRM.Infrastructure.Endpoints;
using EntApp.Modules.HR.Infrastructure.Endpoints;
using EntApp.Modules.Finance.Infrastructure.Endpoints;
using EntApp.Modules.Inventory.Infrastructure.Endpoints;
using EntApp.Modules.Sales.Infrastructure.Endpoints;
using EntApp.Modules.Procurement.Infrastructure.Endpoints;
using EntApp.Modules.TaskManagement.Infrastructure.Endpoints;
using EntApp.WebAPI.Endpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

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

    // ── CORS ─────────────────────────────────────────────────
    builder.Services.AddEntAppCors(builder.Configuration);

    // ── Authentication (Keycloak JWT + API Key) ─────────────
    builder.Services.AddKeycloakAuthentication(builder.Configuration);
    builder.Services.AddApiKeyAuthentication();

    // ── Authorization (Permission-based RBAC) ───────────────
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("SuperAdmin", policy => policy.RequireRole("superadmin"))
        .AddPolicy("TenantAdmin", policy => policy.RequireRole("tenant_admin", "superadmin"));

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

    // ── Event Bus ────────────────────────────────────────────
    builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

    // ── Module Auto-Discovery ────────────────────────────────
    // Tüm modül assembly'leri — IModuleInstaller otomatik keşfedilir
    builder.Services.AddModules(
        builder.Configuration,
        typeof(EntApp.Shared.Infrastructure.Modules.ModuleRegistration).Assembly,
        typeof(EntApp.Modules.IAM.Infrastructure.IamModuleInstaller).Assembly,
        typeof(EntApp.Modules.Configuration.Infrastructure.ConfigModuleInstaller).Assembly,
        typeof(EntApp.Modules.AI.Infrastructure.AiModuleInstaller).Assembly,
        typeof(EntApp.Modules.MultiTenancy.Infrastructure.TenantModuleInstaller).Assembly,
        typeof(EntApp.Modules.Audit.Infrastructure.AuditModuleInstaller).Assembly,
        typeof(EntApp.Modules.Workflow.Infrastructure.WorkflowModuleInstaller).Assembly,
        typeof(EntApp.Modules.CRM.Infrastructure.CrmModuleInstaller).Assembly,
        typeof(EntApp.Modules.HR.Infrastructure.HrModuleInstaller).Assembly,
        typeof(EntApp.Modules.Finance.Infrastructure.FinanceModuleInstaller).Assembly,
        typeof(EntApp.Modules.Inventory.Infrastructure.InventoryModuleInstaller).Assembly,
        typeof(EntApp.Modules.Sales.Infrastructure.SalesModuleInstaller).Assembly,
        typeof(EntApp.Modules.Procurement.Infrastructure.ProcurementModuleInstaller).Assembly,
        typeof(EntApp.Modules.TaskManagement.Infrastructure.TaskManagementModuleInstaller).Assembly,
        typeof(EntApp.Modules.Notification.Infrastructure.NotificationModuleInstaller).Assembly,
        typeof(EntApp.Modules.Localization.Infrastructure.LocalizationModuleInstaller).Assembly,
        typeof(EntApp.Modules.FileManagement.Infrastructure.FileModuleInstaller).Assembly
    );

    // ── Dynamic CRUD Engine ──────────────────────────────────
    // [DynamicEntity] attribute'ü olan entity'ler için otomatik metadata + CRUD
    builder.Services.AddDynamicCrud(
        typeof(EntApp.Modules.IAM.Infrastructure.IamModuleInstaller).Assembly,
        typeof(EntApp.Modules.Configuration.Domain.Entities.Country).Assembly,
        typeof(EntApp.Modules.AI.Domain.Entities.AiModel).Assembly,
        typeof(EntApp.Modules.CRM.Domain.Entities.CustomerBase).Assembly,
        typeof(EntApp.Modules.Sales.Domain.Entities.SalesOrderBase).Assembly
    );

    // Dynamic entity → DbContext eşleştirmesi
    builder.Services.AddDynamicDbContext<
        EntApp.Modules.Configuration.Infrastructure.Persistence.ConfigDbContext>(
        typeof(EntApp.Modules.Configuration.Domain.Entities.Country),
        typeof(EntApp.Modules.Configuration.Domain.Entities.City),
        typeof(EntApp.Modules.Configuration.Domain.Entities.Currency)
    );

    builder.Services.AddDynamicDbContext<
        EntApp.Modules.AI.Infrastructure.Persistence.AiDbContext>(
        typeof(EntApp.Modules.AI.Domain.Entities.AiModel),
        typeof(EntApp.Modules.AI.Domain.Entities.PromptTemplate)
    );

    builder.Services.AddDynamicDbContext<
        EntApp.Modules.CRM.Infrastructure.Persistence.CrmDbContext>(
        typeof(EntApp.Modules.CRM.Domain.Entities.CustomerBase),
        typeof(EntApp.Modules.CRM.Domain.Entities.ContactBase),
        typeof(EntApp.Modules.CRM.Domain.Entities.OpportunityBase),
        typeof(EntApp.Modules.CRM.Domain.Entities.ActivityBase)
    );

    builder.Services.AddDynamicDbContext<
        EntApp.Modules.Sales.Infrastructure.Persistence.SalesDbContext>(
        typeof(EntApp.Modules.Sales.Domain.Entities.SalesOrderBase),
        typeof(EntApp.Modules.Sales.Domain.Entities.OrderItemBase)
    );

    // ═════════════════════════════════════════════════════════
    //  MIDDLEWARE PIPELINE
    // ═════════════════════════════════════════════════════════
    var app = builder.Build();

    // ── Log loaded modules ──────────────────────────────────
    app.LogLoadedModules();

    // ── Database Auto-Create (Development) ─────────────────
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;

        // Her modül için schema ve tabloları oluştur (idempotent)
        async Task EnsureModuleTables<T>(IServiceProvider provider) where T : DbContext
        {
            var db = provider.GetRequiredService<T>();
            try
            {
                var creator = ((IInfrastructure<IServiceProvider>)db.Database).Instance
                    .GetRequiredService<IRelationalDatabaseCreator>();
                await creator.CreateTablesAsync();
                Log.Information("[DB] Tables created for {Module}", typeof(T).Name);
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P07") // already exists
            {
                Log.Debug("[DB] Tables already exist for {Module}", typeof(T).Name);
            }
        }

        await EnsureModuleTables<EntApp.Modules.MultiTenancy.Infrastructure.Persistence.TenantDbContext>(sp);
        await EnsureModuleTables<EntApp.Modules.Audit.Infrastructure.Persistence.AuditDbContext>(sp);
        await EnsureModuleTables<EntApp.Modules.Workflow.Infrastructure.Persistence.WorkflowDbContext>(sp);
        await EnsureModuleTables<EntApp.Modules.CRM.Infrastructure.Persistence.CrmDbContext>(sp);
        await EnsureModuleTables<EntApp.Modules.HR.Infrastructure.Persistence.HrDbContext>(sp);
        await EnsureModuleTables<EntApp.Modules.Finance.Infrastructure.Persistence.FinanceDbContext>(sp);
        await EnsureModuleTables<EntApp.Modules.Inventory.Infrastructure.Persistence.InventoryDbContext>(sp);
        await EnsureModuleTables<EntApp.Modules.Sales.Infrastructure.Persistence.SalesDbContext>(sp);
        await EnsureModuleTables<EntApp.Modules.Procurement.Infrastructure.Persistence.ProcurementDbContext>(sp);
        await EnsureModuleTables<EntApp.Modules.TaskManagement.Infrastructure.Persistence.TaskManagementDbContext>(sp);
        await EnsureModuleTables<EntApp.Modules.Configuration.Infrastructure.Persistence.ConfigDbContext>(sp);
        await EnsureModuleTables<EntApp.Modules.IAM.Infrastructure.Persistence.IamDbContext>(sp);
        await EnsureModuleTables<EntApp.Modules.Notification.Infrastructure.Persistence.NotificationDbContext>(sp);
        await EnsureModuleTables<EntApp.Modules.Localization.Infrastructure.Persistence.LocalizationDbContext>(sp);
        await EnsureModuleTables<EntApp.Modules.FileManagement.Infrastructure.Persistence.FileDbContext>(sp);

        Log.Information("[DB] All module schemas ensured.");
    }

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

    // ── Security Headers (OWASP) ────────────────────────────
    app.UseSecurityHeaders(enableHsts: !app.Environment.IsDevelopment());

    // ── HTTPS Redirection (Production) ──────────────────────
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    // ── Request Logging ─────────────────────────────────────
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseSerilogRequestLogging();

    // ── CORS ─────────────────────────────────────────────────
    app.UseEntAppCors();

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

    // ── AI Endpoints ─────────────────────────────────────────
    app.MapAiEndpoints();
    app.MapPromptEndpoints();
    app.MapUsageEndpoints();
    app.MapWorkflowEndpoints();
    app.MapCrmEndpoints();
    app.MapHrEndpoints();
    app.MapFinanceEndpoints();
    app.MapInventoryEndpoints();
    app.MapSalesEndpoints();
    app.MapProcurementEndpoints();
    app.MapTaskManagementEndpoints();
    app.MapAdminEndpoints();
    app.MapTenantManageEndpoints();

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
