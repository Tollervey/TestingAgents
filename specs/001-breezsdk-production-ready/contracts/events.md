# Event Schema Documentation

**Date**: 2026-01-24 | **Plan**: [../plan.md](../plan.md)

## Overview

This document defines the event schema for the BreezSDK Extensions package. Events enable reactive programming patterns and integration with external systems via webhooks.

---

## Event Distribution Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         BreezSDK                                    │
│                    (Native Event Listener)                          │
└────────────────────────────┬────────────────────────────────────────┘
                             │ SdkEvent
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    BreezEventProcessor                              │
│                 (IHostedService - background)                       │
└────────────────────────────┬────────────────────────────────────────┘
                             │ PaymentEvent (Channel)
                             ▼
         ┌───────────────────┴───────────────────┐
         │                                       │
         ▼                                       ▼
┌─────────────────────┐             ┌─────────────────────┐
│  IPaymentEventHandler│             │   Webhook Delivery   │
│   (DI-registered)    │             │   (if configured)    │
└─────────────────────┘             └─────────────────────┘
```

---

## Event Types

### InvoiceCreated

Raised when a new invoice is generated.

**Trigger**: `IBreezSdkService.CreateInvoiceAsync()` completes successfully.

**Schema**:
```json
{
  "eventType": "invoice_created",
  "paymentHash": "a1b2c3d4e5f6...",
  "invoice": "lnbc50n1pj9...",
  "amountSat": 5000,
  "description": "Payment for article access",
  "expiresAt": "2026-01-24T11:30:00Z",
  "timestamp": "2026-01-24T10:30:00Z",
  "correlationId": "req-12345"
}
```

**Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| eventType | string | Yes | Always `invoice_created` |
| paymentHash | string | Yes | 64-character hex payment hash |
| invoice | string | Yes | BOLT11 encoded invoice |
| amountSat | integer | Yes | Amount in satoshis |
| description | string | No | Human-readable description |
| expiresAt | datetime | Yes | Invoice expiration time |
| timestamp | datetime | Yes | Event generation time |
| correlationId | string | No | Distributed tracing ID |

---

### PaymentReceived

Raised when an incoming payment is detected but not yet confirmed.

**Trigger**: SDK raises `SdkEvent.PaymentPending` for receive direction.

**Schema**:
```json
{
  "eventType": "payment_received",
  "paymentHash": "a1b2c3d4e5f6...",
  "amountSat": 5000,
  "timestamp": "2026-01-24T10:31:00Z",
  "correlationId": "req-12345"
}
```

**Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| eventType | string | Yes | Always `payment_received` |
| paymentHash | string | Yes | 64-character hex payment hash |
| amountSat | integer | Yes | Amount received in satoshis |
| timestamp | datetime | Yes | Event generation time |
| correlationId | string | No | Distributed tracing ID |

---

### PaymentConfirmed

Raised when a payment is fully confirmed.

**Trigger**: SDK raises `SdkEvent.PaymentSucceeded`.

**Schema**:
```json
{
  "eventType": "payment_confirmed",
  "paymentHash": "a1b2c3d4e5f6...",
  "amountSat": 5000,
  "preimage": "b2c3d4e5f6a1...",
  "feeSat": 10,
  "timestamp": "2026-01-24T10:31:30Z",
  "correlationId": "req-12345"
}
```

**Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| eventType | string | Yes | Always `payment_confirmed` |
| paymentHash | string | Yes | 64-character hex payment hash |
| amountSat | integer | Yes | Amount confirmed in satoshis |
| preimage | string | Yes | 64-character hex preimage |
| feeSat | integer | No | Fee paid (for outgoing payments) |
| timestamp | datetime | Yes | Event generation time |
| correlationId | string | No | Distributed tracing ID |

---

### PaymentFailed

Raised when a payment fails permanently.

**Trigger**: SDK raises `SdkEvent.PaymentFailed`.

**Schema**:
```json
{
  "eventType": "payment_failed",
  "paymentHash": "a1b2c3d4e5f6...",
  "errorCode": 3002,
  "errorMessage": "Payment route not found",
  "isRetryable": false,
  "timestamp": "2026-01-24T10:31:30Z",
  "correlationId": "req-12345"
}
```

**Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| eventType | string | Yes | Always `payment_failed` |
| paymentHash | string | Yes | 64-character hex payment hash |
| errorCode | integer | Yes | BreezErrorCode value |
| errorMessage | string | Yes | Human-readable error |
| isRetryable | boolean | Yes | Whether retry may succeed |
| timestamp | datetime | Yes | Event generation time |
| correlationId | string | No | Distributed tracing ID |

---

### InvoiceExpired

Raised when an invoice expires without payment.

**Trigger**: Background expiration check or SDK event.

**Schema**:
```json
{
  "eventType": "invoice_expired",
  "paymentHash": "a1b2c3d4e5f6...",
  "expiredAt": "2026-01-24T11:30:00Z",
  "timestamp": "2026-01-24T11:30:01Z",
  "correlationId": "req-12345"
}
```

**Fields**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| eventType | string | Yes | Always `invoice_expired` |
| paymentHash | string | Yes | 64-character hex payment hash |
| expiredAt | datetime | Yes | Original expiration time |
| timestamp | datetime | Yes | Event generation time |
| correlationId | string | No | Distributed tracing ID |

---

## Event Handler Interface

Applications handle events by implementing `IPaymentEventHandler`:

```csharp
namespace Breez.Sdk.Liquid.Extensions.Core.Abstractions;

/// <summary>
/// Handler for payment events.
/// </summary>
public interface IPaymentEventHandler
{
    /// <summary>
    /// Handles a payment event.
    /// </summary>
    /// <param name="event">The payment event to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task HandleAsync(PaymentEvent @event, CancellationToken cancellationToken = default);
}
```

**Registration**:
```csharp
services.AddBreezSdk(options => { ... });

// Register event handlers (all will be invoked for each event)
services.AddTransient<IPaymentEventHandler, EmailNotificationHandler>();
services.AddTransient<IPaymentEventHandler, DatabaseLoggingHandler>();
services.AddTransient<IPaymentEventHandler, AnalyticsHandler>();
```

**Handler Example**:
```csharp
public class EmailNotificationHandler : IPaymentEventHandler
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailNotificationHandler> _logger;

    public EmailNotificationHandler(
        IEmailService emailService,
        ILogger<EmailNotificationHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(PaymentEvent @event, CancellationToken ct)
    {
        if (@event is PaymentConfirmed confirmed)
        {
            _logger.LogInformation(
                "Sending payment confirmation email for {PaymentHash}",
                confirmed.PaymentHash[..8]);

            await _emailService.SendPaymentConfirmationAsync(
                confirmed.PaymentHash,
                confirmed.AmountSat,
                ct);
        }
    }
}
```

---

## Webhook Delivery

When `BreezSdkOptions.WebhookUrl` is configured, events are delivered via HTTP POST with HMAC signature.

### Delivery Flow

1. Event raised by SDK
2. Event processor converts to `PaymentEvent`
3. Webhook serialized to JSON
4. HMAC-SHA256 signature computed
5. HTTP POST to configured URL
6. Retry on failure (3 attempts, exponential backoff)

### Headers

| Header | Description | Example |
|--------|-------------|---------|
| `Content-Type` | Content type | `application/json` |
| `X-Breez-Signature` | HMAC signature | `sha256=a1b2c3...` |
| `X-Breez-Timestamp` | Unix timestamp | `1706094600` |
| `X-Breez-Event-Id` | Unique delivery ID | `evt_a1b2c3d4` |
| `X-Breez-Event-Type` | Event type | `payment_confirmed` |

### Signature Validation

```csharp
public static bool ValidateWebhook(
    HttpRequest request,
    string secret)
{
    var signature = request.Headers["X-Breez-Signature"].FirstOrDefault();
    var timestamp = request.Headers["X-Breez-Timestamp"].FirstOrDefault();

    if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp))
        return false;

    // Check timestamp freshness (5 minute window)
    if (!long.TryParse(timestamp, out var ts))
        return false;

    var eventTime = DateTimeOffset.FromUnixTimeSeconds(ts);
    if (Math.Abs((DateTimeOffset.UtcNow - eventTime).TotalMinutes) > 5)
        return false;

    // Read body
    request.Body.Position = 0;
    using var reader = new StreamReader(request.Body);
    var payload = reader.ReadToEndAsync().Result;

    // Compute expected signature
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{payload}"));
    var expected = $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";

    // Constant-time comparison
    return CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(expected),
        Encoding.UTF8.GetBytes(signature));
}
```

### Retry Policy

| Attempt | Delay | Total Time |
|---------|-------|------------|
| 1 | 0 | 0s |
| 2 | 2s | 2s |
| 3 | 8s | 10s |

After 3 failures, the webhook is logged and abandoned. Events are not queued; if webhook delivery fails, the event is lost. For critical applications, implement `IPaymentEventHandler` for persistent processing.

---

## Error Codes Reference

| Code | Name | Description | Retryable |
|------|------|-------------|-----------|
| 1001 | ConfigurationMissing | Required config missing | No |
| 1002 | ConfigurationInvalid | Config validation failed | No |
| 1003 | MnemonicMissing | Mnemonic not provided | No |
| 1004 | ApiKeyMissing | API key not provided | No |
| 2001 | SdkNotConnected | SDK not initialized | Yes |
| 2002 | ConnectionTimeout | Connection timed out | Yes |
| 2003 | ConnectionFailed | Could not connect | Yes |
| 2004 | SdkDisconnected | SDK disconnected | Yes |
| 3001 | InvoiceCreationFailed | Failed to create invoice | Yes |
| 3002 | PaymentFailed | Payment could not complete | Depends |
| 3003 | InsufficientFunds | Not enough balance | No |
| 3004 | AmountBelowMinimum | Amount too small | No |
| 3005 | AmountAboveMaximum | Amount too large | No |
| 3006 | InvoiceExpired | Invoice has expired | No |
| 3007 | InvalidInvoice | Invoice format invalid | No |
| 4001 | PersistenceFailed | Database operation failed | Yes |
| 4002 | PaymentNotFound | Payment not in database | No |
| 4003 | DuplicatePayment | Payment already exists | No |
| 5001 | RateLimited | Too many requests | Yes |
| 5002 | ServiceUnavailable | Service temporarily down | Yes |
| 5003 | NetworkError | Network connectivity issue | Yes |

---

## Testing Events

The offline/mock mode generates synthetic events for testing:

```csharp
services.AddBreezSdkOffline(options =>
{
    options.SimulatePaymentDelay = TimeSpan.FromSeconds(2);
    options.SimulateFailureRate = 0.1; // 10% of payments fail
});
```

**Mock Event Sequence**:
1. `CreateInvoiceAsync()` → raises `InvoiceCreated`
2. After configured delay → raises `PaymentReceived`
3. Shortly after → raises `PaymentConfirmed` (or `PaymentFailed` if simulating failure)

This allows testing event handlers without network connectivity.
