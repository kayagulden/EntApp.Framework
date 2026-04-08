using Elsa.Studio.Dashboard.Extensions;
using Elsa.Studio.Shell;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Login.BlazorWasm.Extensions;
using Elsa.Studio.Login.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// ═══════════════════════════════════════════════════════════════
//  EntApp Workflow Designer — Elsa Studio (Blazor WASM)
//  Backend API: http://localhost:5212
//  Designer UI: http://localhost:5280
// ═══════════════════════════════════════════════════════════════

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuration = builder.Configuration;

// Root components
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.RootComponents.RegisterCustomElsaStudioElements();

// Login module (Shell'in ihtiyaç duyduğu DI servislerini kaydeder)
builder.Services.AddLoginModule().UseElsaIdentity();

// Dev: Her zaman authenticated admin user döndüren AuthenticationStateProvider
// (Login formunu bypass eder — Blazor AuthorizeView çalışır)
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider, AlwaysAuthenticatedStateProvider>();

// API Key delegating handler — her isteğe otomatik admin API key ekler
builder.Services.AddTransient<ApiKeyDelegatingHandler>();

// Shell + Remote Backend — API Key ile auth (login form bypass)
var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(ApiKeyDelegatingHandler),
};

builder.Services.AddCore();
builder.Services.AddShell();
builder.Services.AddRemoteBackend(backendApiConfig);

// Modules
builder.Services.AddDashboardModule();
builder.Services.AddWorkflowsModule();

// Build + Run
var app = builder.Build();

var startupTaskRunner = app.Services.GetRequiredService<IStartupTaskRunner>();
await startupTaskRunner.RunStartupTasksAsync();

await app.RunAsync();

/// <summary>
/// Her HTTP isteğine Elsa admin API key ekleyen DelegatingHandler.
/// Development ortamı için — production'da login-based auth kullanılmalıdır.
/// </summary>
public class ApiKeyDelegatingHandler : DelegatingHandler
{
    // Elsa AdminApiKeyProvider'ın varsayılan anahtarı: Guid.Empty
    private const string AdminApiKey = "00000000-0000-0000-0000-000000000000";


    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("ApiKey", AdminApiKey);
        return base.SendAsync(request, cancellationToken);
    }
}

/// <summary>
/// Her zaman authenticated admin user döndüren AuthenticationStateProvider.
/// Development ortamı için — login formunu bypass eder.
/// </summary>
public class AlwaysAuthenticatedStateProvider : Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider
{
    public override Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> GetAuthenticationStateAsync()
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "admin"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "admin"),
            new System.Security.Claims.Claim("permissions", "*"),
        };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "ApiKey");
        var user = new System.Security.Claims.ClaimsPrincipal(identity);
        return Task.FromResult(new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(user));
    }
}
