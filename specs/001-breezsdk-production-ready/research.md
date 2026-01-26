# Research: BreezSDK Production-Ready NuGet Package

**Date**: 2026-01-24 | **Plan**: [plan.md](./plan.md)

## Executive Summary

This document resolves all technical decisions required for implementing the BreezSDK production-ready NuGet package suite. Research covers package structure, dependency management, persistence strategy, and migration approach.

---

## Research Area 1: Package Architecture

### Decision: Multi-Package Suite with Layered Dependencies

**Rationale**: The package suite follows a layered dependency model where consumers only install what they need:

```
                    ┌─────────────────────────────────┐
                    │  Breez.Sdk.Liquid.Extensions    │
                    │         .Umbraco                │
                    └───────────────┬─────────────────┘
                                    │ depends on
                    ┌───────────────┴─────────────────┐
                    │  Breez.Sdk.Liquid.Extensions    │
                    │       .AspNetCore               │
                    └───────────────┬─────────────────┘
                                    │ depends on
┌───────────────────┬───────────────┼───────────────────┬───────────────────┐
│    .SqlServer     │  .PostgreSql  │     .Sqlite       │    (Others)       │
└─────────┬─────────┴───────┬───────┴─────────┬─────────┴─────────┬─────────┘
          │                 │                 │                   │
          └─────────────────┴────────┬────────┴───────────────────┘
                                     │ depends on
                    ┌────────────────┴────────────────┐
                    │   Breez.Sdk.Liquid.Extensions   │
                    │           .Core                 │
                    └─────────────────────────────────┘
```

**Alternatives Considered**:

1. **Single monolithic package**: Rejected because it forces all consumers to install persistence providers, Umbraco dependencies, and ASP.NET Core packages even when not needed. Violates YAGNI principle.

2. **Two packages (Core + All Integrations)**: Rejected because mixing persistence providers creates version conflicts and bloated dependency graphs.

3. **Micro-packages per feature**: Rejected because it creates too many packages to manage and version. Over-engineering for current requirements.

**Package Details**:

| Package | Dependencies | Target Consumers |
|---------|--------------|------------------|
| `.Core` | Breez.Sdk.Liquid, Polly, OpenTelemetry.Api, Microsoft.Extensions.* | All .NET 8+ applications |
| `.AspNetCore` | .Core, Microsoft.AspNetCore.* | Web applications |
| `.SqlServer` | .Core, EF Core SqlServer | Enterprise deployments |
| `.PostgreSql` | .Core, EF Core PostgreSQL | Cloud-native deployments |
| `.Sqlite` | .Core, EF Core SQLite | Single-instance deployments |
| `.Umbraco` | .AspNetCore, Umbraco.Cms.* | Umbraco CMS sites |

---

## Research Area 2: Framework Version Strategy

### Decision: .NET 8.0 Minimum with Multi-Targeting

**Rationale**:
- .NET 8 is LTS (Long-Term Support) until November 2026
- .NET 9 is current but STS (Standard Term Support)
- Multi-targeting ensures compatibility while enabling new features

**Implementation**:
```xml
<TargetFrameworks>net8.0;net9.0</TargetFrameworks>
```

**Conditional Compilation** (where needed):
```csharp
#if NET9_0_OR_GREATER
    // Use .NET 9 specific APIs
#else
    // Fallback for .NET 8
#endif
```

**Alternatives Considered**:

1. **.NET 9 only**: Rejected because it excludes production applications on LTS release.
2. **.NET 6/7 support**: Rejected because .NET 6 EOL is November 2024 and .NET 7 already EOL.
3. **.NET Standard 2.1**: Rejected because BreezSDK bindings require .NET Core 3.1+ features.

---

## Research Area 3: Resilience Library Selection

### Decision: Polly v8 with Resilience Pipelines

**Rationale**: Polly is the de facto standard for .NET resilience. Version 8 introduces simpler APIs with `ResiliencePipeline`.

**Existing Pattern** (to preserve):
```csharp
// Current Polly v8 usage in codebase
private static readonly ResiliencePipeline ConnectPolicy = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(2),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    })
    .AddTimeout(TimeSpan.FromSeconds(30))
    .Build();
```

**Standard Policies for Package**:

| Policy Name | Retries | Backoff | Timeout | Use Case |
|-------------|---------|---------|---------|----------|
| `Connect` | 3 | Exponential + Jitter | 30s | SDK initialization |
| `PaymentOperation` | 2 | Linear 2s | 15s | Invoice creation, payments |
| `Query` | 1 | None | 10s | Read operations |
| `Webhook` | 3 | Exponential | 30s | Webhook delivery |

**Alternatives Considered**:

1. **Microsoft.Extensions.Resilience**: Rejected because Polly integration is already established and working; switching provides no benefit.
2. **Custom retry logic**: Rejected because reinventing resilience patterns is error-prone and violates DRY.

---

## Research Area 4: Observability Strategy

### Decision: OpenTelemetry API with Optional Exporters

**Rationale**: OpenTelemetry is the industry standard for observability. Using the API package keeps the core lightweight; consumers configure their preferred exporters.

**Instrumentation Points**:

| Category | Metric/Trace | Name | Tags |
|----------|--------------|------|------|
| Connection | Trace | `breez.sdk.connect` | `network`, `status` |
| Invoice | Counter | `breez.invoice.created` | `network`, `type` |
| Invoice | Trace | `breez.sdk.create_invoice` | `amount_sat`, `type` |
| Payment | Counter | `breez.payment.received` | `network`, `status` |
| Payment | Histogram | `breez.payment.latency` | `operation`, `status` |
| Health | Gauge | `breez.sdk.connected` | `network` |

**Activity Source** (existing):
```csharp
private static readonly ActivitySource ActivitySource = new("BreezSdkService");
```

**Alternatives Considered**:

1. **Application Insights SDK directly**: Rejected because it locks consumers to Azure; OpenTelemetry is portable.
2. **No instrumentation**: Rejected because observability is a functional requirement (FR-023, FR-024).

---

## Research Area 5: Persistence Abstraction

### Decision: Repository Pattern with EF Core Provider Packages

**Rationale**: EF Core's provider model maps naturally to separate NuGet packages. Each provider package contains migrations and DbContext configuration for that database.

**Repository Interface** (minimal):
```csharp
public interface IPaymentRepository
{
    Task<PaymentState?> GetByHashAsync(string paymentHash, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentState>> GetByStatusAsync(PaymentStatus status, int limit, CancellationToken ct = default);
    Task<PaymentState> AddAsync(PaymentState payment, CancellationToken ct = default);
    Task<PaymentState> UpdateAsync(PaymentState payment, CancellationToken ct = default);
    Task<bool> ExistsAsync(string paymentHash, CancellationToken ct = default);
}
```

**In-Memory Default**:
- Ships with `.Core` package
- Uses `ConcurrentDictionary` for thread safety
- Suitable for development, testing, and stateless scenarios
- Data lost on process restart (by design)

**Alternatives Considered**:

1. **SQLite default in Core**: Rejected because it adds file system dependencies and complicates testing.
2. **Abstract DbContext in Core**: Rejected because it couples Core to EF Core; repository pattern is cleaner.
3. **No default persistence**: Rejected because developers need working code out of the box.

---

## Research Area 6: Configuration Design

### Decision: Options Pattern with Validation

**Rationale**: The `IOptions<T>` pattern is the .NET standard. Adding validation ensures fail-fast behavior for misconfiguration.

**Options Class**:
```csharp
public class BreezSdkOptions
{
    public const string SectionName = "BreezSdk";

    public string? ApiKey { get; set; }
    public string? Mnemonic { get; set; }
    public BreezNetwork Network { get; set; } = BreezNetwork.Mainnet;
    public string? WorkingDirectory { get; set; }
    public string? WebhookUrl { get; set; }
    public string? WebhookSecret { get; set; }
    public int MaxInvoiceAmountSat { get; set; } = 10_000_000;
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
}

public class CircuitBreakerOptions
{
    public int FailureThreshold { get; set; } = 5;
    public int SamplingDurationSeconds { get; set; } = 60;
    public int BreakDurationSeconds { get; set; } = 30;
}
```

**Registration Pattern**:
```csharp
services.AddBreezSdk(options =>
{
    options.ApiKey = configuration["BreezSdk:ApiKey"];
    options.Network = BreezNetwork.Testnet;
});

// Or from configuration section
services.AddBreezSdk(configuration.GetSection("BreezSdk"));
```

**Alternatives Considered**:

1. **Static configuration**: Rejected because it prevents runtime configuration and testing.
2. **Separate options per feature**: Rejected because BreezSDK configuration is cohesive; splitting adds complexity.

---

## Research Area 7: Event System Design

### Decision: Channel-Based Event Distribution

**Rationale**: `System.Threading.Channels` provides high-performance, bounded event distribution with backpressure. Consumers can use async enumeration for natural processing.

**Event Handler Interface**:
```csharp
public interface IPaymentEventHandler
{
    Task HandleAsync(PaymentEvent @event, CancellationToken ct = default);
}
```

**Event Types**:
```csharp
public abstract record PaymentEvent(string PaymentHash, DateTimeOffset Timestamp);
public record PaymentCreated(string PaymentHash, ulong AmountSat, string Invoice, DateTimeOffset Timestamp)
    : PaymentEvent(PaymentHash, Timestamp);
public record PaymentReceived(string PaymentHash, ulong AmountSat, DateTimeOffset Timestamp)
    : PaymentEvent(PaymentHash, Timestamp);
public record PaymentFailed(string PaymentHash, string Reason, DateTimeOffset Timestamp)
    : PaymentEvent(PaymentHash, Timestamp);
```

**Channel Configuration**:
```csharp
Channel.CreateBounded<PaymentEvent>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = false,
    SingleWriter = true
});
```

**Alternatives Considered**:

1. **Plain C# events**: Rejected because they're synchronous and don't support backpressure.
2. **IObservable/Rx**: Rejected because it adds a significant dependency for simple use case.
3. **MediatR notifications**: Rejected because it adds external dependency; channels are built-in.

---

## Research Area 8: Exception Hierarchy

### Decision: Categorized Exceptions with Error Codes

**Rationale**: Typed exceptions enable targeted error handling. Error codes enable localization and log aggregation.

**Exception Hierarchy**:
```
BreezSdkException (base)
├── ConfigurationException
│   └── MissingConfigurationException
├── ConnectionException
│   ├── SdkNotConnectedException
│   └── ConnectionTimeoutException
├── PaymentException
│   ├── InvoiceCreationException
│   ├── PaymentProcessingException
│   └── InsufficientFundsException
└── TransientException (retryable)
    └── RateLimitedException
```

**Error Code Structure**:
```csharp
public enum BreezErrorCode
{
    // Configuration (1xxx)
    ConfigurationMissing = 1001,
    ConfigurationInvalid = 1002,

    // Connection (2xxx)
    SdkNotConnected = 2001,
    ConnectionTimeout = 2002,

    // Payment (3xxx)
    InvoiceCreationFailed = 3001,
    PaymentFailed = 3002,
    InsufficientFunds = 3003,

    // Transient (4xxx)
    RateLimited = 4001,
    ServiceUnavailable = 4002
}
```

**Alternatives Considered**:

1. **Result<T> pattern only**: Rejected because exceptions provide natural error propagation; Result<T> still useful for expected failures.
2. **Single exception type with enum**: Rejected because it prevents targeted catch blocks.

---

## Research Area 9: Migration Strategy

### Decision: Clean Break with Migration Guide

**Rationale**: Per specification clarification, a clean break with new package names and APIs is preferred over backward compatibility hacks.

**Migration Path**:

| Old (Umbraco-coupled) | New (Platform-agnostic) |
|-----------------------|-------------------------|
| `Umbraco.Community.Bitcoin.LightningPayments.Core` | `Breez.Sdk.Liquid.Extensions.Core` |
| `LightningPaymentsSettings` | `BreezSdkOptions` |
| `IBreezSdkService` | `IBreezSdkService` (same interface, new namespace) |
| `builder.AddLightningPayments()` | `services.AddBreezSdk()` |
| `builder.AddLightningPaymentsOffline()` | `services.AddBreezSdkOffline()` |

**Migration Guide Contents**:
1. Package replacement instructions
2. Namespace change list
3. Configuration key mapping
4. Breaking API changes
5. Database migration (if schema changes)

**Alternatives Considered**:

1. **Backward compatibility layer**: Rejected per specification; adds maintenance burden.
2. **In-place upgrade**: Rejected because package name changes require explicit replacement anyway.

---

## Research Area 10: Webhook Security

### Decision: HMAC-SHA256 Signature Validation

**Rationale**: Per specification clarification (FR-018a), webhooks use HMAC signature validation with a shared secret.

**Implementation**:
```csharp
public static bool ValidateSignature(string payload, string signature, string secret)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();
    return CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(computedSignature),
        Encoding.UTF8.GetBytes(signature.Replace("sha256=", ""))
    );
}
```

**Middleware Pattern**:
```csharp
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api/breez/webhook"),
    branch => branch.UseMiddleware<WebhookValidationMiddleware>()
);
```

**Alternatives Considered**:

1. **No validation**: Rejected because webhooks are security-critical.
2. **API key in header**: Rejected because HMAC provides payload integrity verification.
3. **mTLS**: Rejected because it requires certificate management; HMAC is simpler for this use case.

---

## Research Area 11: Health Check Design

### Decision: Multi-Level Health Checks

**Rationale**: Kubernetes-style health checks require liveness (is the process running?) and readiness (is the service ready to handle requests?).

**Health Check Levels**:

| Endpoint | Check | Pass Criteria |
|----------|-------|---------------|
| `/health/live` | Process running | Always passes if reachable |
| `/health/ready` | SDK connected | `IsConnectedAsync()` returns true |
| `/health/startup` | Initialization complete | SDK initialized without error |

**Implementation**:
```csharp
public class BreezSdkHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            var isConnected = await _sdkService.IsConnectedAsync();
            return isConnected
                ? HealthCheckResult.Healthy("SDK connected")
                : HealthCheckResult.Degraded("SDK not connected");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SDK error", ex);
        }
    }
}
```

**Alternatives Considered**:

1. **Single health endpoint**: Rejected because it conflates liveness and readiness.
2. **Detailed health with payment history**: Rejected because it exposes sensitive information.

---

## Research Area 12: Testing Strategy

### Decision: Mock Builder Pattern with SDK Abstraction

**Rationale**: The existing `BreezSdkServiceMockBuilder` pattern is effective. Extending it for the new package structure maintains consistency.

**Test Categories**:

| Category | Location | Dependencies |
|----------|----------|--------------|
| Unit tests | `*.Tests` per package | Moq, xUnit |
| Integration tests | `.Integration.Tests` | TestContainers, real databases |
| E2E tests | `.Integration.Tests/EndToEnd` | Testnet SDK connection |

**Mock Builder Pattern**:
```csharp
var service = new BreezSdkServiceMockBuilder()
    .WithConnectedSdk()
    .WithInvoiceSuccessFlow(expectedHash: "abc123")
    .Build();
```

**Shared Test Utilities**:
- `FakeBreezSdkWrapper` - In-memory SDK simulation
- `MockBreezSdkBuilder` - Fluent mock configuration
- `TestServiceCollectionExtensions` - DI setup for tests

**Alternatives Considered**:

1. **No test utilities package**: Rejected because test code duplication across packages.
2. **Full SDK simulation**: Rejected because BreezSDK behavior is complex; mocking at wrapper level is sufficient.

---

## Conclusions

All technical decisions are now resolved:

1. **Package Structure**: 7-package suite with layered dependencies
2. **Framework**: .NET 8.0 minimum, multi-target to .NET 9.0
3. **Resilience**: Polly v8 with standard policies
4. **Observability**: OpenTelemetry API with configurable exporters
5. **Persistence**: Repository pattern with EF Core provider packages
6. **Configuration**: IOptions<T> with validation
7. **Events**: System.Threading.Channels for distribution
8. **Exceptions**: Categorized hierarchy with error codes
9. **Migration**: Clean break with comprehensive guide
10. **Webhooks**: HMAC-SHA256 signature validation
11. **Health Checks**: Kubernetes-style liveness/readiness
12. **Testing**: Mock builder pattern with shared utilities

Ready to proceed to Phase 1: Design & Contracts.
