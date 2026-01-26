# BreezSDK Migration Guide

## Overview

This guide helps you migrate from the original Umbraco-coupled BreezSDK implementation to the new modular package suite. The new architecture separates concerns into multiple packages, allowing you to use BreezSDK in any .NET application, not just Umbraco.

### What Changed?

**Before (Single Package)**
- Single `Umbraco.Community.Bitcoin.LightningPayments` package
- Tightly coupled to Umbraco CMS
- Limited persistence options
- Mixed concerns (domain, infrastructure, UI)

**After (Multi-Package Suite)**
- **Core**: `Breez.Sdk.Liquid.Extensions.Core` - Domain logic, abstractions
- **ASP.NET Core**: `Breez.Sdk.Liquid.Extensions.AspNetCore` - Health checks, webhooks
- **Persistence**: SQLite, SQL Server, PostgreSQL packages (optional)
- **Umbraco**: `Breez.Sdk.Liquid.Extensions.Umbraco` - CMS integration
- Framework-agnostic design with clean architecture

---

## 1. Package Installation

### For Umbraco Applications

```bash
# Install core and Umbraco integration
dotnet add package Breez.Sdk.Liquid.Extensions.Umbraco

# Optional: Add database persistence (choose one)
dotnet add package Breez.Sdk.Liquid.Extensions.Sqlite
# OR
dotnet add package Breez.Sdk.Liquid.Extensions.SqlServer
# OR
dotnet add package Breez.Sdk.Liquid.Extensions.PostgreSql
```

The Umbraco package automatically includes Core and ASP.NET Core packages as dependencies.

### For Non-Umbraco ASP.NET Core Applications

```bash
# Install core and ASP.NET Core integration
dotnet add package Breez.Sdk.Liquid.Extensions.Core
dotnet add package Breez.Sdk.Liquid.Extensions.AspNetCore

# Optional: Add database persistence
dotnet add package Breez.Sdk.Liquid.Extensions.Sqlite
```

### For Console/Worker Applications

```bash
# Install only the core package
dotnet add package Breez.Sdk.Liquid.Extensions.Core

# Optional: Add database persistence
dotnet add package Breez.Sdk.Liquid.Extensions.Sqlite
```

---

## 2. Namespace Changes

Update all using statements in your code:

### Old Namespaces
```csharp
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Models;
```

### New Namespaces
```csharp
// Core abstractions and domain
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.Core.Domain.Events;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;

// ASP.NET Core (health checks, webhooks)
using Breez.Sdk.Liquid.Extensions.AspNetCore.HealthChecks;
using Breez.Sdk.Liquid.Extensions.AspNetCore.Extensions;

// Umbraco integration
using Breez.Sdk.Liquid.Extensions.Umbraco;
using Breez.Sdk.Liquid.Extensions.Umbraco.Composers;
```

---

## 3. Configuration Migration

### Old Configuration (Umbraco-specific)
```json
{
  "LightningPayments": {
    "BreezApiKey": "your-api-key",
    "WalletMnemonic": "your twelve word mnemonic phrase here",
    "Network": "Testnet",
    "WorkingDirectory": "C:\\breez-data"
  }
}
```

### New Configuration (Standardized)
```json
{
  "BreezSdk": {
    "ApiKey": "your-api-key",
    "Mnemonic": "your twelve word mnemonic phrase here",
    "Network": "Testnet",
    "WorkingDirectory": "C:\\breez-data",
    "WebhookUrl": "https://example.com/api/breez/webhook",
    "WebhookSecret": "your-webhook-secret",
    "MaxInvoiceAmountSat": 10000000,
    "MaxInvoiceDescriptionLength": 200,
    "ConnectionTimeoutSeconds": 30,
    "OfflineMode": false,
    "CircuitBreaker": {
      "FailureThreshold": 5,
      "SamplingDurationSeconds": 60,
      "BreakDurationSeconds": 30,
      "MinimumThroughput": 10
    },
    "Reconnection": {
      "MaxRetries": 3,
      "BaseDelaySeconds": 2,
      "MaxDelaySeconds": 30
    }
  }
}
```

**Key Changes:**
- Section name: `LightningPayments` → `BreezSdk`
- Property names: `BreezApiKey` → `ApiKey`, `WalletMnemonic` → `Mnemonic`
- New properties: `WebhookUrl`, `WebhookSecret`, circuit breaker configuration
- Added `OfflineMode` flag for development/testing

---

## 4. Service Registration Changes

### Old Registration (Umbraco Composer)
```csharp
public class LightningPaymentsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Manual service registration
        builder.Services.AddSingleton<IBreezSdkService, BreezSdkService>();
        // ... other services
    }
}
```

### New Registration (Umbraco)

**Option 1: Automatic (Recommended)**
```csharp
// No code needed! BreezSdkComposer runs automatically
// Just ensure appsettings.json has the "BreezSdk" section
```

**Option 2: Manual with Custom Configuration**
```csharp
using Breez.Sdk.Liquid.Extensions.Umbraco;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

public class MyCustomComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddBreezSdk(options =>
        {
            // Override configuration values
            options.Network = BreezNetwork.Mainnet;
            options.MaxInvoiceAmountSat = 5_000_000;
        });
    }
}
```

### New Registration (ASP.NET Core)

**In `Program.cs`:**
```csharp
using Breez.Sdk.Liquid.Extensions.Core.Extensions;
using Breez.Sdk.Liquid.Extensions.AspNetCore.Extensions;
using Breez.Sdk.Liquid.Extensions.AspNetCore.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Option 1: From configuration
builder.Services.AddBreezSdk(
    builder.Configuration.GetSection("BreezSdk"));

// Option 2: Inline configuration
builder.Services.AddBreezSdk(options =>
{
    options.ApiKey = "your-api-key";
    options.Mnemonic = "your twelve word mnemonic phrase here";
    options.Network = BreezNetwork.Testnet;
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddBreezSdkHealthCheck("breez-sdk", tags: new[] { "ready" });

var app = builder.Build();

// Map webhook endpoints
app.MapBreezSdkEndpoints("/api/breez");

// Map health check endpoints
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
```

### Offline Mode for Development

```csharp
// Umbraco
builder.AddBreezSdkOffline(options =>
{
    options.ApiKey = "test-key";
    options.Mnemonic = "test mnemonic phrase with twelve words here";
    options.OfflineMockBalanceSat = 500_000;
});

// ASP.NET Core
builder.Services.AddBreezSdkOffline(options =>
{
    options.OfflineMockBalanceSat = 500_000;
    options.OfflineSimulateDelayMs = 100;
});
```

---

## 5. Interface Changes

### IBreezSdkService Changes

#### Old Interface
```csharp
public interface IBreezSdkService : IAsyncDisposable
{
    Task<string> CreateInvoiceAsync(ulong amountSat, string description, CancellationToken ct = default);
    Task<string> CreateBolt12OfferAsync(ulong amountSat, string description, CancellationToken ct = default);
    Task<bool> IsConnectedAsync(CancellationToken ct = default);
    Task<string?> TryExtractPaymentHashAsync(string invoice, CancellationToken ct = default);
    Task<DateTimeOffset?> TryExtractInvoiceExpiryAsync(string invoice, CancellationToken ct = default);
    Task<Payment?> GetPaymentByHashAsync(string paymentHash, CancellationToken ct = default);
    Task<List<Payment>> GetPaymentsAsync(CancellationToken ct = default);
    Task<long> GetReceiveFeeQuoteAsync(ulong amountSat, bool bolt12 = false, CancellationToken ct = default);
    Task<RecommendedFees?> GetRecommendedFeesAsync(CancellationToken ct = default);
}
```

#### New Interface
```csharp
public interface IBreezSdkService
{
    // Connection management
    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default);

    // Invoice creation returns OperationResult with domain model
    Task<OperationResult<Invoice>> CreateInvoiceAsync(
        ulong amountSat,
        string? description = null,
        uint? expirySec = null,
        CancellationToken cancellationToken = default);

    // Payment retrieval uses domain model
    Task<PaymentState?> GetPaymentByHashAsync(
        string paymentHash,
        CancellationToken cancellationToken = default);

    // Paginated payment history
    Task<IReadOnlyList<PaymentState>> GetPaymentHistoryAsync(
        int offset = 0,
        int limit = 50,
        CancellationToken cancellationToken = default);

    // Balance query returns OperationResult
    Task<OperationResult<ulong>> GetBalanceAsync(
        CancellationToken cancellationToken = default);
}
```

**Key Changes:**
1. **Result Type Pattern**: Methods return `OperationResult<T>` instead of throwing exceptions
2. **Domain Models**: Uses `Invoice`, `PaymentState` instead of SDK types
3. **Explicit Connection**: `ConnectAsync()` / `DisconnectAsync()` methods
4. **Pagination**: `GetPaymentHistoryAsync()` replaces `GetPaymentsAsync()`
5. **Removed**: BOLT12 offers, fee quotes, invoice parsing (moved to lower-level wrapper)

#### Migration Example

**Before:**
```csharp
try
{
    var invoice = await _breezSdkService.CreateInvoiceAsync(10000, "Test payment");
    var paymentHash = await _breezSdkService.TryExtractPaymentHashAsync(invoice);

    // Wait for payment...
    var payment = await _breezSdkService.GetPaymentByHashAsync(paymentHash);
}
catch (Exception ex)
{
    // Handle errors
}
```

**After:**
```csharp
// Create invoice
var result = await _breezSdkService.CreateInvoiceAsync(
    amountSat: 10000,
    description: "Test payment",
    expirySec: 3600);

if (!result.IsSuccess)
{
    // Handle error using result.Error
    _logger.LogError("Invoice creation failed: {ErrorMessage}", result.Error.Message);
    return;
}

var invoice = result.Value;
var paymentHash = invoice.PaymentHash;

// Wait for payment...
var payment = await _breezSdkService.GetPaymentByHashAsync(paymentHash);

if (payment != null && payment.Status == PaymentStatus.Confirmed)
{
    _logger.LogInformation("Payment received: {Amount} sat", payment.AmountSat);
}
```

### IPaymentRepository (New)

The new architecture introduces a repository pattern for payment persistence:

```csharp
public interface IPaymentRepository
{
    Task<PaymentState?> GetByHashAsync(string paymentHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentState>> GetByStatusAsync(PaymentStatus status, int limit = 100, CancellationToken cancellationToken = default);
    Task<PaymentState> AddAsync(PaymentState payment, CancellationToken cancellationToken = default);
    Task<PaymentState> UpdateAsync(PaymentState payment, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string paymentHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentState>> GetAllAsync(int offset = 0, int limit = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentState>> GetByDateRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}
```

**Default Implementation**: `InMemoryPaymentRepository` (registered automatically)

**Usage Example:**
```csharp
public class PaymentHistoryService
{
    private readonly IPaymentRepository _paymentRepository;

    public PaymentHistoryService(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<IReadOnlyList<PaymentState>> GetRecentPayments()
    {
        return await _paymentRepository.GetAllAsync(offset: 0, limit: 10);
    }

    public async Task<IReadOnlyList<PaymentState>> GetPendingPayments()
    {
        return await _paymentRepository.GetByStatusAsync(PaymentStatus.Pending);
    }
}
```

---

## 6. Event Handling Changes

### Old Event Handling
The old implementation had limited event support. Payment status was typically polled.

### New Event Handling System

The new architecture provides a strongly-typed event system using `PaymentEvent` records:

#### Event Types

```csharp
// Base event
public abstract record PaymentEvent
{
    public required string PaymentHash { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string? CorrelationId { get; init; }
}

// Concrete events
public record InvoiceCreated : PaymentEvent
{
    public required string Invoice { get; init; }
    public required ulong AmountSat { get; init; }
    public string? Description { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}

public record PaymentReceived : PaymentEvent
{
    public required ulong AmountSat { get; init; }
}

public record PaymentConfirmed : PaymentEvent
{
    public required ulong AmountSat { get; init; }
    public required string Preimage { get; init; }
    public ulong? FeeSat { get; init; }
}

public record PaymentFailed : PaymentEvent
{
    public required BreezErrorCode ErrorCode { get; init; }
    public required string Reason { get; init; }
    public required bool IsRetryable { get; init; }
}

public record InvoiceExpired : PaymentEvent
{
    public required DateTimeOffset ExpiredAt { get; init; }
}
```

#### Event Handler Implementation

```csharp
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain.Events;

public class PaymentNotificationHandler : IPaymentEventHandler
{
    private readonly ILogger<PaymentNotificationHandler> _logger;
    private readonly IEmailService _emailService;

    public PaymentNotificationHandler(
        ILogger<PaymentNotificationHandler> logger,
        IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task HandleAsync(PaymentEvent paymentEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing payment event {EventType} for payment {PaymentHash}",
            paymentEvent.GetType().Name,
            paymentEvent.PaymentHash[..8]);

        switch (paymentEvent)
        {
            case InvoiceCreated created:
                _logger.LogInformation("Invoice created: {Amount} sat", created.AmountSat);
                break;

            case PaymentReceived received:
                _logger.LogInformation("Payment received: {Amount} sat", received.AmountSat);
                break;

            case PaymentConfirmed confirmed:
                _logger.LogInformation(
                    "Payment confirmed: {Amount} sat, preimage: {Preimage}",
                    confirmed.AmountSat,
                    confirmed.Preimage[..8]);
                await _emailService.SendPaymentConfirmationAsync(confirmed);
                break;

            case PaymentFailed failed:
                _logger.LogWarning(
                    "Payment failed: {Reason} (code: {ErrorCode}, retryable: {IsRetryable})",
                    failed.Reason,
                    failed.ErrorCode,
                    failed.IsRetryable);
                break;

            case InvoiceExpired expired:
                _logger.LogInformation("Invoice expired at {ExpiredAt}", expired.ExpiredAt);
                break;
        }
    }
}

// Register handler in DI
builder.Services.AddSingleton<IPaymentEventHandler, PaymentNotificationHandler>();
```

#### Event Channel

For advanced scenarios, inject `IPaymentEventChannel` to publish custom events:

```csharp
public class CustomPaymentService
{
    private readonly IPaymentEventChannel _eventChannel;

    public CustomPaymentService(IPaymentEventChannel eventChannel)
    {
        _eventChannel = eventChannel;
    }

    public async Task PublishCustomEvent(string paymentHash)
    {
        var customEvent = new PaymentConfirmed
        {
            PaymentHash = paymentHash,
            AmountSat = 10000,
            Preimage = "...",
            Timestamp = DateTimeOffset.UtcNow
        };

        await _eventChannel.PublishAsync(customEvent);
    }
}
```

---

## 7. Persistence Migration

### Old Persistence
The old implementation had no abstraction for payment persistence. Payment data was retrieved directly from the SDK.

### New Persistence Options

#### Default: In-Memory (Development)

Automatically registered, no configuration needed:

```csharp
// Automatically uses InMemoryPaymentRepository
builder.Services.AddBreezSdk(configuration.GetSection("BreezSdk"));
```

#### SQLite (Recommended for Production)

```csharp
using Breez.Sdk.Liquid.Extensions.Sqlite.Extensions;

// After AddBreezSdk()
builder.Services.AddBreezSdkSqlite("Data Source=payments.db");
```

**Migrations:**
```bash
# In your project directory
dotnet ef migrations add InitialCreate --context SqlitePaymentDbContext
dotnet ef database update --context SqlitePaymentDbContext
```

#### SQL Server (Enterprise)

```csharp
using Breez.Sdk.Liquid.Extensions.SqlServer.Extensions;

builder.Services.AddBreezSdkSqlServer(
    "Server=localhost;Database=BreezPayments;Trusted_Connection=true;");
```

#### PostgreSQL (Cloud)

```csharp
using Breez.Sdk.Liquid.Extensions.PostgreSql.Extensions;

builder.Services.AddBreezSdkPostgreSql(
    "Host=localhost;Database=breez;Username=postgres;Password=yourpassword");
```

#### Custom Persistence

Implement `IPaymentRepository` for custom storage:

```csharp
public class CosmosDbPaymentRepository : IPaymentRepository
{
    // Implement interface methods
}

// Register custom repository BEFORE AddBreezSdk()
builder.Services.AddSingleton<IPaymentRepository, CosmosDbPaymentRepository>();
builder.Services.AddBreezSdk(configuration.GetSection("BreezSdk"));
```

---

## 8. ASP.NET Core Integration

### Health Checks

The new packages provide production-ready health checks:

```csharp
using Breez.Sdk.Liquid.Extensions.AspNetCore.Extensions;

// In Program.cs
builder.Services.AddHealthChecks()
    .AddBreezSdkHealthCheck("breez-sdk", tags: new[] { "ready" });

var app = builder.Build();

// Map health check endpoints
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Liveness only checks basic app health
});
```

**Health Check Output:**
```json
{
  "status": "Healthy",
  "results": {
    "breez-sdk": {
      "status": "Healthy",
      "description": "SDK connected",
      "data": {
        "duration_ms": 45.2
      }
    }
  }
}
```

### Webhook Endpoints

Map webhook endpoints to receive payment notifications:

```csharp
using Breez.Sdk.Liquid.Extensions.AspNetCore.Endpoints;

var app = builder.Build();

// Map webhook endpoints at /api/breez/webhook
app.MapBreezSdkEndpoints("/api/breez");
```

**Configure webhook in appsettings.json:**
```json
{
  "BreezSdk": {
    "WebhookUrl": "https://yourdomain.com/api/breez/webhook",
    "WebhookSecret": "your-secret-for-hmac-validation"
  }
}
```

**Webhook payload example:**
```json
{
  "eventType": "payment_confirmed",
  "paymentHash": "abc123...",
  "amountSat": 10000,
  "preimage": "def456...",
  "timestamp": "2026-01-26T12:34:56Z"
}
```

---

## 9. Umbraco-Specific Migration

### Automatic Composer

The new package includes `BreezSdkComposer` which runs automatically:

```csharp
// This runs automatically when Umbraco starts
public class BreezSdkComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddBreezSdk();
        builder.Components().Append<BreezSdkComponent>();
    }
}
```

**What it does:**
1. Reads configuration from `appsettings.json` → `BreezSdk` section
2. Registers all BreezSDK services
3. Adds health checks
4. Starts the SDK connection via `BreezSdkComponent`

### Custom Composer

Override defaults with a custom composer:

```csharp
using Breez.Sdk.Liquid.Extensions.Umbraco;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

public class CustomBreezComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Disable automatic composer by not using it
        // Register services manually
        builder.AddBreezSdk(options =>
        {
            options.Network = BreezNetwork.Mainnet;
            options.MaxInvoiceAmountSat = 1_000_000;
        });

        // Add database persistence
        builder.Services.AddBreezSdkSqlite("Data Source=umbraco-payments.db");
    }
}
```

### Dependency Injection in Umbraco

Inject services in controllers, components, or notification handlers:

```csharp
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;

public class PaymentApiController : UmbracoApiController
{
    private readonly IBreezSdkService _breezSdkService;
    private readonly IPaymentRepository _paymentRepository;

    public PaymentApiController(
        IBreezSdkService breezSdkService,
        IPaymentRepository paymentRepository)
    {
        _breezSdkService = breezSdkService;
        _paymentRepository = paymentRepository;
    }

    [HttpPost("create-invoice")]
    public async Task<IActionResult> CreateInvoice(ulong amountSat, string description)
    {
        var result = await _breezSdkService.CreateInvoiceAsync(amountSat, description);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error.Message);
        }

        return Ok(new { invoice = result.Value.Bolt11, paymentHash = result.Value.PaymentHash });
    }

    [HttpGet("payment/{paymentHash}")]
    public async Task<IActionResult> GetPayment(string paymentHash)
    {
        var payment = await _paymentRepository.GetByHashAsync(paymentHash);

        if (payment == null)
        {
            return NotFound();
        }

        return Ok(payment);
    }
}
```

---

## 10. Complete Migration Checklist

### Phase 1: Installation
- [ ] Install new packages (`Breez.Sdk.Liquid.Extensions.Umbraco` or individual packages)
- [ ] Remove old `Umbraco.Community.Bitcoin.LightningPayments` package
- [ ] Update `Directory.Packages.props` or `.csproj` versions

### Phase 2: Configuration
- [ ] Rename configuration section: `LightningPayments` → `BreezSdk`
- [ ] Update property names: `BreezApiKey` → `ApiKey`, `WalletMnemonic` → `Mnemonic`
- [ ] Add new optional properties (`WebhookUrl`, `CircuitBreaker`, etc.)
- [ ] Verify `OfflineMode` flag for development environments

### Phase 3: Code Updates
- [ ] Update all `using` statements to new namespaces
- [ ] Replace `IBreezSdkService` method calls with new signatures
- [ ] Handle `OperationResult<T>` return types instead of exceptions
- [ ] Update domain models: `Payment` → `PaymentState`, SDK types → domain types
- [ ] Remove calls to removed methods (BOLT12, fee quotes, invoice parsing)

### Phase 4: Service Registration
- [ ] Remove manual service registration if using Umbraco (automatic composer)
- [ ] For ASP.NET Core, add `AddBreezSdk()` to `Program.cs`
- [ ] Choose persistence option (in-memory, SQLite, SQL Server, PostgreSQL)
- [ ] Register persistence package AFTER `AddBreezSdk()`

### Phase 5: Event Handling
- [ ] Implement `IPaymentEventHandler` for payment notifications
- [ ] Register event handlers in DI container
- [ ] Replace polling logic with event-driven approach
- [ ] Test event delivery with webhook endpoints

### Phase 6: Health Checks & Webhooks (ASP.NET Core)
- [ ] Add `AddBreezSdkHealthCheck()` to health checks builder
- [ ] Map health check endpoints (`/health/ready`, `/health/live`)
- [ ] Map webhook endpoints with `MapBreezSdkEndpoints()`
- [ ] Configure webhook URL and secret in settings

### Phase 7: Testing
- [ ] Test invoice creation with new `OperationResult<Invoice>` return type
- [ ] Verify payment retrieval from repository
- [ ] Test event handlers receive payment events
- [ ] Verify health check endpoints return correct status
- [ ] Test webhook endpoint with sample payloads
- [ ] Run integration tests with offline mode

### Phase 8: Database Migration (if using persistence)
- [ ] Run EF Core migrations for chosen database
- [ ] Migrate existing payment data (if any) to new schema
- [ ] Verify repository queries return expected results
- [ ] Set up backup/restore procedures

### Phase 9: Deployment
- [ ] Update environment-specific appsettings (staging, production)
- [ ] Deploy to staging environment
- [ ] Monitor health checks and logs
- [ ] Verify webhook delivery
- [ ] Deploy to production
- [ ] Monitor production health and payment events

---

## Troubleshooting

### Service Not Registered

**Error:** `Unable to resolve service for type 'IBreezSdkService'`

**Solution:**
```csharp
// Ensure AddBreezSdk() is called
builder.Services.AddBreezSdk(configuration.GetSection("BreezSdk"));

// For Umbraco, ensure BreezSdkComposer is loaded (automatic)
```

### Configuration Validation Fails

**Error:** `OptionsValidationException: DataAnnotation validation failed for members: 'ApiKey'`

**Solution:**
```json
{
  "BreezSdk": {
    "ApiKey": "must-not-be-empty",
    "Mnemonic": "must be at least 12 words"
  }
}
```

### Persistence Repository Not Found

**Error:** `Unable to resolve service for type 'IPaymentRepository'`

**Solution:**
```csharp
// Add persistence package AFTER AddBreezSdk()
builder.Services.AddBreezSdk(configuration.GetSection("BreezSdk"));
builder.Services.AddBreezSdkSqlite("Data Source=payments.db");
```

### Webhook Returns 400 Bad Request

**Error:** Webhook endpoint returns `Invalid JSON` or `Missing required field`

**Solution:**
Ensure webhook payload matches the expected format:
```json
{
  "eventType": "payment_confirmed",
  "paymentHash": "64-character-hex-string",
  "amountSat": 10000,
  "timestamp": "2026-01-26T12:34:56Z"
}
```

### Health Check Shows Unhealthy

**Error:** `/health/ready` returns `Unhealthy` status

**Solution:**
1. Check SDK connection: `await breezSdkService.IsConnectedAsync()`
2. Verify configuration: API key, mnemonic, network
3. Check logs for connection errors
4. Test with offline mode to isolate SDK issues

### Offline Mode Not Working

**Error:** SDK tries to connect despite `OfflineMode: true`

**Solution:**
```csharp
// Ensure using AddBreezSdkOffline() method
builder.Services.AddBreezSdkOffline(configuration.GetSection("BreezSdk"));

// OR set in configuration
{
  "BreezSdk": {
    "OfflineMode": true
  }
}
```

---

## Migration Support

For additional help:
- **GitHub Issues**: [Link to repository issues]
- **Documentation**: [Link to full documentation]
- **Community**: [Link to Discord/Slack/Forum]

---

## Summary

The new modular architecture provides:
- **Flexibility**: Use BreezSDK in any .NET application
- **Clean Architecture**: Separation of concerns (domain, infrastructure, UI)
- **Persistence Options**: Choose SQLite, SQL Server, PostgreSQL, or custom
- **Event-Driven**: React to payment events with strongly-typed handlers
- **Production-Ready**: Health checks, webhooks, circuit breakers, retries
- **Testability**: Offline mode, mocks, and repository abstractions

Follow this guide to migrate incrementally, starting with package installation and configuration, then updating code and testing thoroughly before production deployment.
