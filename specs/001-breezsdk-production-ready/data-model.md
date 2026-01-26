# Data Model: BreezSDK Production-Ready NuGet Package

**Date**: 2026-01-24 | **Plan**: [plan.md](./plan.md)

## Overview

This document defines the domain entities, value objects, and relationships for the BreezSDK Extensions package suite. These models are platform-agnostic and suitable for any .NET 8+ application.

---

## Core Entities

### PaymentState

Represents the persistent state of a payment tracked by the application.

```csharp
namespace Breez.Sdk.Liquid.Extensions.Core.Domain;

/// <summary>
/// Represents the persistent state of a payment transaction.
/// </summary>
public class PaymentState
{
    /// <summary>
    /// Unique identifier for the payment record.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The payment hash (hex-encoded) that uniquely identifies this payment in the Lightning network.
    /// </summary>
    public required string PaymentHash { get; init; }

    /// <summary>
    /// Current status of the payment.
    /// </summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// Payment amount in satoshis.
    /// </summary>
    public required ulong AmountSat { get; init; }

    /// <summary>
    /// Human-readable description of the payment.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The BOLT11 invoice string (if applicable).
    /// </summary>
    public string? Invoice { get; init; }

    /// <summary>
    /// Type of payment (e.g., Paywall, TipJar, Custom).
    /// </summary>
    public PaymentKind Kind { get; init; } = PaymentKind.Custom;

    /// <summary>
    /// UTC timestamp when the payment was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// UTC timestamp when the payment was confirmed (if applicable).
    /// </summary>
    public DateTimeOffset? ConfirmedAt { get; set; }

    /// <summary>
    /// UTC timestamp when the payment expires (for invoices).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Preimage revealed upon successful payment (hex-encoded).
    /// </summary>
    public string? Preimage { get; set; }

    /// <summary>
    /// Fee paid in satoshis (for outgoing payments).
    /// </summary>
    public ulong? FeeSat { get; set; }

    /// <summary>
    /// Custom metadata dictionary for application-specific data.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }
}
```

**Database Mapping (EF Core)**:
```csharp
public class PaymentStateConfiguration : IEntityTypeConfiguration<PaymentState>
{
    public void Configure(EntityTypeBuilder<PaymentState> builder)
    {
        builder.ToTable("PaymentStates");
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.PaymentHash).IsUnique();
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.CreatedAt);
        builder.Property(p => p.PaymentHash).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Invoice).HasMaxLength(2000);
        builder.Property(p => p.Preimage).HasMaxLength(64);
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.CorrelationId).HasMaxLength(64);
        builder.Property(p => p.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions) ?? new())
            .HasColumnType("nvarchar(max)");
    }
}
```

---

### Invoice

Represents a BOLT11 or BOLT12 invoice/offer.

```csharp
namespace Breez.Sdk.Liquid.Extensions.Core.Domain;

/// <summary>
/// Represents a Lightning invoice or offer.
/// </summary>
public record Invoice
{
    /// <summary>
    /// The payment hash (hex-encoded) that identifies this invoice.
    /// </summary>
    public required string PaymentHash { get; init; }

    /// <summary>
    /// The encoded invoice or offer string (BOLT11 or BOLT12).
    /// </summary>
    public required string Destination { get; init; }

    /// <summary>
    /// Type of invoice.
    /// </summary>
    public InvoiceType Type { get; init; } = InvoiceType.Bolt11;

    /// <summary>
    /// Requested amount in satoshis.
    /// </summary>
    public required ulong AmountSat { get; init; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// UTC timestamp when the invoice was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// UTC timestamp when the invoice expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Fees that will be charged on successful payment (in satoshis).
    /// </summary>
    public ulong? FeeSat { get; init; }
}

public enum InvoiceType
{
    Bolt11,
    Bolt12Offer
}
```

---

### OperationResult<T>

Represents the outcome of an SDK operation.

```csharp
namespace Breez.Sdk.Liquid.Extensions.Core.Domain;

/// <summary>
/// Represents the result of an SDK operation.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public sealed class OperationResult<T>
{
    private OperationResult(T? value, OperationError? error, bool isSuccess)
    {
        Value = value;
        Error = error;
        IsSuccess = isSuccess;
    }

    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The success value (if successful).
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// The error details (if failed).
    /// </summary>
    public OperationError? Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static OperationResult<T> Success(T value) => new(value, null, true);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static OperationResult<T> Failure(OperationError error) => new(default, error, false);

    /// <summary>
    /// Creates a failed result from an exception.
    /// </summary>
    public static OperationResult<T> Failure(Exception ex, bool isRetryable = false)
        => new(default, OperationError.FromException(ex, isRetryable), false);
}

/// <summary>
/// Describes an operation error.
/// </summary>
public sealed record OperationError
{
    public required BreezErrorCode Code { get; init; }
    public required string Message { get; init; }
    public bool IsRetryable { get; init; }
    public Exception? Exception { get; init; }

    public static OperationError FromException(Exception ex, bool isRetryable = false)
        => new()
        {
            Code = MapExceptionToCode(ex),
            Message = ex.Message,
            IsRetryable = isRetryable,
            Exception = ex
        };

    private static BreezErrorCode MapExceptionToCode(Exception ex) => ex switch
    {
        ConfigurationException => BreezErrorCode.ConfigurationInvalid,
        ConnectionException => BreezErrorCode.SdkNotConnected,
        PaymentException => BreezErrorCode.PaymentFailed,
        _ => BreezErrorCode.ServiceUnavailable
    };
}
```

---

## Value Objects

### PaymentStatus

```csharp
namespace Breez.Sdk.Liquid.Extensions.Core.Domain;

/// <summary>
/// Payment status enumeration.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is awaiting confirmation.
    /// </summary>
    Pending,

    /// <summary>
    /// Payment has been confirmed.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Payment failed permanently.
    /// </summary>
    Failed,

    /// <summary>
    /// Payment invoice has expired.
    /// </summary>
    Expired,

    /// <summary>
    /// Payment was refunded.
    /// </summary>
    Refunded
}
```

### PaymentKind

```csharp
namespace Breez.Sdk.Liquid.Extensions.Core.Domain;

/// <summary>
/// Categorizes payments by business purpose.
/// </summary>
public enum PaymentKind
{
    /// <summary>
    /// Generic payment without specific category.
    /// </summary>
    Custom,

    /// <summary>
    /// Payment for content access (paywall).
    /// </summary>
    Paywall,

    /// <summary>
    /// Voluntary payment (tip jar).
    /// </summary>
    TipJar,

    /// <summary>
    /// E-commerce purchase.
    /// </summary>
    Purchase,

    /// <summary>
    /// Subscription payment.
    /// </summary>
    Subscription
}
```

### BreezNetwork

```csharp
namespace Breez.Sdk.Liquid.Extensions.Core.Configuration;

/// <summary>
/// Supported BreezSDK networks.
/// </summary>
public enum BreezNetwork
{
    /// <summary>
    /// Production Bitcoin mainnet.
    /// </summary>
    Mainnet,

    /// <summary>
    /// Bitcoin testnet for development.
    /// </summary>
    Testnet,

    /// <summary>
    /// Local regtest network.
    /// </summary>
    Regtest
}
```

### BreezErrorCode

```csharp
namespace Breez.Sdk.Liquid.Extensions.Core.Domain;

/// <summary>
/// Error codes for BreezSDK operations.
/// </summary>
public enum BreezErrorCode
{
    // Configuration errors (1xxx)
    ConfigurationMissing = 1001,
    ConfigurationInvalid = 1002,
    MnemonicMissing = 1003,
    ApiKeyMissing = 1004,

    // Connection errors (2xxx)
    SdkNotConnected = 2001,
    ConnectionTimeout = 2002,
    ConnectionFailed = 2003,
    SdkDisconnected = 2004,

    // Payment errors (3xxx)
    InvoiceCreationFailed = 3001,
    PaymentFailed = 3002,
    InsufficientFunds = 3003,
    AmountBelowMinimum = 3004,
    AmountAboveMaximum = 3005,
    InvoiceExpired = 3006,
    InvalidInvoice = 3007,

    // Persistence errors (4xxx)
    PersistenceFailed = 4001,
    PaymentNotFound = 4002,
    DuplicatePayment = 4003,

    // Transient errors (5xxx)
    RateLimited = 5001,
    ServiceUnavailable = 5002,
    NetworkError = 5003
}
```

---

## Event Models

### PaymentEvent (Base)

```csharp
namespace Breez.Sdk.Liquid.Extensions.Core.Domain;

/// <summary>
/// Base class for payment events.
/// </summary>
public abstract record PaymentEvent
{
    /// <summary>
    /// The payment hash identifying the payment.
    /// </summary>
    public required string PaymentHash { get; init; }

    /// <summary>
    /// UTC timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }
}
```

### Concrete Events

```csharp
/// <summary>
/// Raised when an invoice is created.
/// </summary>
public sealed record InvoiceCreated : PaymentEvent
{
    public required string Invoice { get; init; }
    public required ulong AmountSat { get; init; }
    public string? Description { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
}

/// <summary>
/// Raised when a payment is received.
/// </summary>
public sealed record PaymentReceived : PaymentEvent
{
    public required ulong AmountSat { get; init; }
    public string? Preimage { get; init; }
}

/// <summary>
/// Raised when a payment is confirmed.
/// </summary>
public sealed record PaymentConfirmed : PaymentEvent
{
    public required ulong AmountSat { get; init; }
    public required string Preimage { get; init; }
    public ulong? FeeSat { get; init; }
}

/// <summary>
/// Raised when a payment fails.
/// </summary>
public sealed record PaymentFailed : PaymentEvent
{
    public required BreezErrorCode ErrorCode { get; init; }
    public required string Reason { get; init; }
    public bool IsRetryable { get; init; }
}

/// <summary>
/// Raised when an invoice expires.
/// </summary>
public sealed record InvoiceExpired : PaymentEvent
{
    public DateTimeOffset ExpiredAt { get; init; }
}
```

---

## Configuration Models

### BreezSdkOptions

```csharp
namespace Breez.Sdk.Liquid.Extensions.Core.Configuration;

/// <summary>
/// Configuration options for BreezSDK.
/// </summary>
public class BreezSdkOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "BreezSdk";

    /// <summary>
    /// Breez API key for authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// BIP39 mnemonic for wallet access.
    /// </summary>
    public string? Mnemonic { get; set; }

    /// <summary>
    /// Target network (Mainnet, Testnet, Regtest).
    /// </summary>
    public BreezNetwork Network { get; set; } = BreezNetwork.Mainnet;

    /// <summary>
    /// Working directory for SDK data files.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Webhook URL for payment notifications.
    /// </summary>
    [Url]
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Shared secret for webhook HMAC validation.
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Maximum invoice amount in satoshis.
    /// </summary>
    [Range(1, long.MaxValue)]
    public ulong MaxInvoiceAmountSat { get; set; } = 10_000_000;

    /// <summary>
    /// Maximum invoice description length.
    /// </summary>
    [Range(1, 1000)]
    public int MaxInvoiceDescriptionLength { get; set; } = 200;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    [Range(5, 300)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable offline/mock mode for development.
    /// </summary>
    public bool OfflineMode { get; set; }

    /// <summary>
    /// Circuit breaker configuration.
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
}

/// <summary>
/// Circuit breaker configuration.
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Number of failures before circuit opens.
    /// </summary>
    [Range(1, 100)]
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Window in seconds to count failures.
    /// </summary>
    [Range(10, 600)]
    public int SamplingDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Duration in seconds to keep circuit open.
    /// </summary>
    [Range(5, 300)]
    public int BreakDurationSeconds { get; set; } = 30;
}
```

---

## Entity Relationships

```
┌─────────────────────────────────────────────────────────────────────┐
│                         PaymentState                                │
│  ┌─────────────┬─────────────┬─────────────┬─────────────────────┐  │
│  │ Id (PK)     │ PaymentHash │ Status      │ AmountSat           │  │
│  │ Guid        │ string[64]  │ enum        │ ulong               │  │
│  │ unique      │ indexed,    │ indexed     │                     │  │
│  │             │ unique      │             │                     │  │
│  └─────────────┴─────────────┴─────────────┴─────────────────────┘  │
│  ┌─────────────┬─────────────┬─────────────┬─────────────────────┐  │
│  │ Invoice     │ Description │ CreatedAt   │ ConfirmedAt         │  │
│  │ string[2000]│ string[500] │ DateTimeOff │ DateTimeOffset?     │  │
│  │ nullable    │ nullable    │ indexed     │                     │  │
│  └─────────────┴─────────────┴─────────────┴─────────────────────┘  │
│  ┌─────────────┬─────────────┬─────────────┬─────────────────────┐  │
│  │ ExpiresAt   │ Preimage    │ FeeSat      │ Metadata            │  │
│  │ DateTimeOff?│ string[64]  │ ulong?      │ JSON dict           │  │
│  │             │ nullable    │             │                     │  │
│  └─────────────┴─────────────┴─────────────┴─────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘

Indexes:
- IX_PaymentStates_PaymentHash (UNIQUE)
- IX_PaymentStates_Status
- IX_PaymentStates_CreatedAt
```

---

## State Transitions

```
┌─────────┐
│ Created │ (Invoice generated, awaiting payment)
└────┬────┘
     │
     ▼
┌─────────┐    payment received    ┌───────────┐
│ Pending │──────────────────────▶│ Succeeded │
└────┬────┘                        └───────────┘
     │
     │ timeout
     ▼
┌─────────┐
│ Expired │
└─────────┘

Alternative paths:
- Pending → Failed (payment error)
- Succeeded → Refunded (refund processed)
```

---

## Validation Rules

### PaymentState

| Field | Rule | Error Code |
|-------|------|------------|
| PaymentHash | Required, 64 hex chars | ConfigurationInvalid |
| AmountSat | > 0, within limits | AmountBelowMinimum / AmountAboveMaximum |
| Description | Max 500 chars, safe chars | ConfigurationInvalid |
| Invoice | Valid BOLT11/BOLT12 format | InvalidInvoice |
| ExpiresAt | Must be future when created | ConfigurationInvalid |

### BreezSdkOptions

| Field | Rule | Error Code |
|-------|------|------------|
| ApiKey | Required for live mode | ApiKeyMissing |
| Mnemonic | Required for live mode | MnemonicMissing |
| Network | Valid enum value | ConfigurationInvalid |
| WorkingDirectory | Must exist, writable | ConfigurationInvalid |
| WebhookUrl | Valid URL format | ConfigurationInvalid |
| MaxInvoiceAmountSat | 1 to max ulong | ConfigurationInvalid |

---

## Serialization

All entities use `System.Text.Json` for serialization with the following options:

```csharp
public static readonly JsonSerializerOptions JsonOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters =
    {
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
    }
};
```

---

## Migration from Existing Models

| Existing Entity | New Entity | Changes |
|-----------------|------------|---------|
| `PaymentState` (Umbraco) | `PaymentState` (Core) | Namespace change; added `CorrelationId`, `Kind` enum extended |
| `PaymentEvent` (internal) | `PaymentEvent` (Core) | Made public; added derived record types |
| `LightningPaymentsSettings` | `BreezSdkOptions` | Renamed; circuit breaker options added |
| `PaymentStatus` | `PaymentStatus` | Added `Refunded` status |
| `PaymentKind` | `PaymentKind` | Added `Purchase`, `Subscription` |
