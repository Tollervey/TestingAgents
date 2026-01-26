using Breez.Sdk.Liquid.Extensions.AspNetCore.Endpoints;
using Breez.Sdk.Liquid.Extensions.AspNetCore.Extensions;
using Breez.Sdk.Liquid.Extensions.Samples.BlazorApp.Components;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add BreezSDK services (offline mode for development)
builder.AddBreezSdkOffline();

// Or use AddBreezSdk() for production
// builder.AddBreezSdk();

// Add health checks
builder.Services.AddHealthChecks()
    .AddBreezSdkHealthCheck();

var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Map BreezSDK endpoints
app.MapBreezSdkEndpoints();

// Health checks
app.MapHealthChecks("/health");

// Blazor components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
