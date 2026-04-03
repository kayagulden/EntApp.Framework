using System.Reflection;

using Serilog;

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
        .WriteTo.Console());

    // ── Health Checks ────────────────────────────────────────
    builder.Services
        .AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing."),
            name: "postgresql",
            tags: ["db", "ready"]);

    // ── Swagger (Development only) ───────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "EntApp API",
            Version = "v1",
            Description = "EntApp Modular Monolith Framework API"
        });
    });

    var app = builder.Build();

    // ── Middleware Pipeline ──────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "EntApp API v1");
        });
    }

    app.UseSerilogRequestLogging();

    // ── Endpoints ───────────────────────────────────────────
    app.MapHealthChecks("/health");

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
