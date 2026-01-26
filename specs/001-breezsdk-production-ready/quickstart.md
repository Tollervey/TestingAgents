# Quickstart: BreezSDK Extensions for .NET

**Date**: 2026-01-24 | **Plan**: [plan.md](./plan.md)

## Overview

This guide shows how to integrate Lightning payments into your .NET application using the BreezSDK Extensions package suite. You'll be accepting Bitcoin payments in under 15 minutes.

---

## Prerequisites

- .NET 8.0 SDK or later
- A Breez API key (obtain from [Breez](https://breez.technology))
- A BIP39 mnemonic (12 or 24 words)

---

## Installation

### Core Package (All Applications)

```bash
dotnet add package Breez.Sdk.Liquid.Extensions.Core
```

### ASP.NET Core Integration

```bash
dotnet add package Breez.Sdk.Liquid.Extensions.AspNetCore
```

### Persistence Providers (Choose One)

```bash
# For single-instance deployments
dotnet add package Breez.Sdk.Liquid.Extensions.Sqlite

# For SQL Server (enterprise)
dotnet add package Breez.Sdk.Liquid.Extensions.SqlServer

# For PostgreSQL (cloud-native)
dotnet add package Breez.Sdk.Liquid.Extensions.PostgreSql
```

### Umbraco CMS

```bash
dotnet add package Breez.Sdk.Liquid.Extensions.Umbraco
```

---

## Basic Setup

### 1. Configure Services

**Program.cs** (ASP.NET Core):
```csharp
using Breez.Sdk.Liquid.Extensions.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add BreezSDK with configuration from appsettings.json
builder.AddBreezSdk();

// Or configure inline
builder.AddBreezSdk(options =>
{
    options.ApiKey = builder.Configuration["BreezSdk:ApiKey"];
    options.Mnemonic = builder.Configuration["BreezSdk:Mnemonic"];
    options.Network = BreezNetwork.Testnet; // Use Testnet for development
});

var app = builder.Build();

// Enable health checks and webhook endpoint
app.MapBreezSdkEndpoints();

app.Run();
```

**Program.cs** (Console Application):
```csharp
using Breez.Sdk.Liquid.Extensions.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddBreezSdk(options =>
        {
            options.ApiKey = context.Configuration["BreezSdk:ApiKey"];
            options.Mnemonic = context.Configuration["BreezSdk:Mnemonic"];
            options.Network = BreezNetwork.Testnet;
        });
    })
    .Build();

await host.RunAsync();
```

### 2. Configuration File

**appsettings.json**:
```json
{
  "BreezSdk": {
    "ApiKey": "your-api-key-here",
    "Mnemonic": "your twelve word mnemonic phrase goes here ...",
    "Network": "Testnet",
    "WorkingDirectory": "./breez-data",
    "MaxInvoiceAmountSat": 10000000,
    "CircuitBreaker": {
      "FailureThreshold": 5,
      "SamplingDurationSeconds": 60,
      "BreakDurationSeconds": 30
    }
  }
}
```

> **Security Note**: Never commit mnemonics or API keys to source control. Use environment variables, user secrets, or Azure Key Vault in production.

**Using Environment Variables**:
```bash
export BreezSdk__ApiKey="your-api-key"
export BreezSdk__Mnemonic="your mnemonic phrase..."
```

**Using User Secrets** (Development):
```bash
dotnet user-secrets set "BreezSdk:ApiKey" "your-api-key"
dotnet user-secrets set "BreezSdk:Mnemonic" "your mnemonic phrase..."
```

---

## Creating Your First Invoice

### Controller Example (ASP.NET Core)

```csharp
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IBreezSdkService _breezService;

    public PaymentsController(IBreezSdkService breezService)
    {
        _breezService = breezService;
    }

    [HttpPost("invoice")]
    public async Task<IActionResult> CreateInvoice(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken ct)
    {
        var result = await _breezService.CreateInvoiceAsync(
            amountSat: request.AmountSat,
            description: request.Description,
            ct);

        if (result.IsFailure)
        {
            return BadRequest(new
            {
                error = result.Error!.Code.ToString(),
                message = result.Error.Message
            });
        }

        return Ok(new
        {
            invoice = result.Value!.Destination,
            paymentHash = result.Value.PaymentHash,
            expiresAt = result.Value.ExpiresAt
        });
    }
}

public record CreateInvoiceRequest(ulong AmountSat, string? Description);
```

### Console Application Example

```csharp
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;

public class PaymentDemo
{
    private readonly IBreezSdkService _breezService;
    private readonly ILogger<PaymentDemo> _logger;

    public PaymentDemo(
        IBreezSdkService breezService,
        ILogger<PaymentDemo> logger)
    {
        _breezService = breezService;
        _logger = logger;
    }

    public async Task CreateInvoiceAsync()
    {
        _logger.LogInformation("Creating invoice for 1000 sats...");

        var result = await _breezService.CreateInvoiceAsync(
            amountSat: 1000,
            description: "Test payment");

        if (result.IsSuccess)
        {
            _logger.LogInformation("Invoice created!");
            _logger.LogInformation("Pay this invoice: {Invoice}", result.Value!.Destination);
            _logger.LogInformation("Payment hash: {Hash}", result.Value.PaymentHash);
        }
        else
        {
            _logger.LogError("Failed to create invoice: {Error}", result.Error!.Message);
        }
    }
}
```

---

## Handling Payment Events

### Implement IPaymentEventHandler

```csharp
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;

public class PaymentNotificationHandler : IPaymentEventHandler
{
    private readonly ILogger<PaymentNotificationHandler> _logger;

    public PaymentNotificationHandler(ILogger<PaymentNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PaymentEvent @event, CancellationToken ct)
    {
        switch (@event)
        {
            case InvoiceCreated created:
                _logger.LogInformation(
                    "Invoice created: {Amount} sats, expires {Expiry}",
                    created.AmountSat,
                    created.ExpiresAt);
                break;

            case PaymentReceived received:
                _logger.LogInformation(
                    "Payment received: {Amount} sats",
                    received.AmountSat);
                // Trigger business logic (e.g., unlock content)
                break;

            case PaymentConfirmed confirmed:
                _logger.LogInformation(
                    "Payment confirmed: {Amount} sats, preimage: {Preimage}",
                    confirmed.AmountSat,
                    confirmed.Preimage[..8] + "...");
                break;

            case PaymentFailed failed:
                _logger.LogWarning(
                    "Payment failed: {Error} (retryable: {Retry})",
                    failed.Reason,
                    failed.IsRetryable);
                break;
        }

        return Task.CompletedTask;
    }
}
```

### Register the Handler

```csharp
services.AddBreezSdk(options => { /* ... */ });
services.AddTransient<IPaymentEventHandler, PaymentNotificationHandler>();
```

---

## Adding Persistence

### SQLite (Single Instance)

```csharp
using Breez.Sdk.Liquid.Extensions.Sqlite;

services.AddBreezSdk(options => { /* ... */ });
services.AddBreezSdkSqlite("Data Source=payments.db");
```

### SQL Server (Enterprise)

```csharp
using Breez.Sdk.Liquid.Extensions.SqlServer;

services.AddBreezSdk(options => { /* ... */ });
services.AddBreezSdkSqlServer(connectionString);
```

### PostgreSQL (Cloud-Native)

```csharp
using Breez.Sdk.Liquid.Extensions.PostgreSql;

services.AddBreezSdk(options => { /* ... */ });
services.AddBreezSdkPostgreSql(connectionString);
```

### Run Migrations

```bash
dotnet ef migrations add InitialCreate --project YourProject
dotnet ef database update --project YourProject
```

---

## Webhook Configuration

### ASP.NET Core Automatic Setup

```csharp
var app = builder.Build();

// Maps /api/breez/webhook with HMAC validation
app.MapBreezSdkEndpoints();
```

### Manual Webhook Endpoint

```csharp
app.MapPost("/api/breez/webhook", async (
    HttpContext context,
    IBreezSdkService breezService) =>
{
    // Validation is automatic via middleware
    var payload = await context.Request.ReadFromJsonAsync<WebhookPayload>();
    // Process webhook...
    return Results.Ok();
}).RequireBreezWebhookValidation();
```

### Configuration

```json
{
  "BreezSdk": {
    "WebhookUrl": "https://your-domain.com/api/breez/webhook",
    "WebhookSecret": "your-shared-secret-for-hmac"
  }
}
```

---

## Development Mode (Offline)

For development without network access:

```csharp
// Use offline/mock mode
services.AddBreezSdkOffline(options =>
{
    options.SimulatePaymentDelay = TimeSpan.FromSeconds(3);
    options.SimulateFailureRate = 0.0; // No failures in dev
});
```

This creates synthetic invoices and simulates payment flows without connecting to the Breez network.

---

## Health Checks

### ASP.NET Core

```csharp
builder.Services.AddHealthChecks()
    .AddBreezSdkHealthCheck();

var app = builder.Build();

app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Liveness doesn't check dependencies
});
```

### Kubernetes Probe Configuration

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
```

---

## Observability

### OpenTelemetry Integration

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("BreezSdkService")
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter("Breez.Sdk.Liquid.Extensions")
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter();
    });
```

### Available Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `breez.invoice.created` | Counter | Invoices created |
| `breez.payment.received` | Counter | Payments received |
| `breez.payment.latency` | Histogram | Operation latency |
| `breez.sdk.connected` | Gauge | Connection status |

---

## Error Handling

### Using OperationResult<T>

```csharp
var result = await _breezService.CreateInvoiceAsync(1000, "Payment");

if (result.IsSuccess)
{
    // Use result.Value
    var invoice = result.Value!;
}
else
{
    // Handle error
    var error = result.Error!;
    _logger.LogError("Error {Code}: {Message}", error.Code, error.Message);

    if (error.IsRetryable)
    {
        // Retry logic
    }
}
```

### Exception Handling

```csharp
try
{
    await _breezService.CreateInvoiceAsync(1000, "Payment");
}
catch (ConfigurationException ex)
{
    // Missing or invalid configuration
    _logger.LogError("Config error: {Message}", ex.Message);
}
catch (ConnectionException ex)
{
    // SDK not connected
    _logger.LogWarning("Connection issue: {Message}", ex.Message);
}
catch (PaymentException ex)
{
    // Payment operation failed
    _logger.LogError("Payment error: {Message}", ex.Message);
}
catch (TransientException ex) when (ex.IsRetryable)
{
    // Transient error - retry
    await Task.Delay(TimeSpan.FromSeconds(2));
    // Retry...
}
```

---

## Complete Example

See the [samples directory](../samples/) for complete working examples:

- **ConsoleApp**: Basic payment flow
- **WebApi**: ASP.NET Core REST API
- **BlazorApp**: Interactive payment UI
- **UmbracoSite**: CMS integration

---

## Next Steps

1. **Security Review**: Ensure secrets are properly protected
2. **Persistence**: Choose and configure a persistence provider
3. **Monitoring**: Set up OpenTelemetry exporters
4. **Testing**: Use offline mode for integration tests
5. **Production**: Switch to Mainnet when ready

---

## Support

- **Documentation**: [docs/](../docs/)
- **Issues**: [GitHub Issues](https://github.com/your-org/breez-sdk-extensions/issues)
- **API Reference**: [API Docs](../docs/api/)
