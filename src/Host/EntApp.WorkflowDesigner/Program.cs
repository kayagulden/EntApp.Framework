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
using Elsa.Studio.Login.HttpMessageHandlers;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// ═══════════════════════════════════════════════════════════════
//  EntApp Workflow Designer — Elsa Studio (Blazor WASM)
//  Backend API: http://localhost:5001
//  Designer UI: http://localhost:5280
// ═══════════════════════════════════════════════════════════════

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuration = builder.Configuration;

// Root components
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.RootComponents.RegisterCustomElsaStudioElements();

// Elsa Login (username/password against Elsa backend Identity)
builder.Services.AddLoginModule().UseElsaIdentity();
var authenticationHandler = typeof(AuthenticatingApiHttpMessageHandler);

// Shell + Remote Backend
var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options => options.AuthenticationHandler = authenticationHandler,
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
