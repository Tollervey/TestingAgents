# Payment Notification System - Quick Reference Guide

**Quick Links**: [Full Patterns](./NOTIFICATION_SYSTEM_PATTERNS.md) | [Config Example](#configuration-example) | [Testing](#testing-checklists)

---

## At a Glance

| Component | Framework | Pattern | Key File |
|-----------|-----------|---------|----------|
| **Email** | MailKit | Async with Polly retry | `IEmailNotificationService` |
| **Webhook** | HttpClient | HMAC-SHA256 signed | `IWebhookDeliveryService` |
| **Retry Policy** | Polly 8.6.5 | Exponential backoff, 5 retries | `NotificationResiliencePolicies` |
| **Persistence** | EF Core 9.0 | Repository pattern | `IPaymentNotificationRepository` |
| **Orchestration** | Hosted service | Scheduled retry | `NotificationRetryBackgroundService` |

---

## Polly Retry Policy Summary

```
Delay sequence: 2s → 4s → 8s → 16s → 32s (with 25% jitter)
Total time:     ~62 seconds base + jitter
Max retries:    5 attempts
Transient errors: 5xx, timeouts, connection refused
```

**Test Policy** (100x faster for unit tests):
```
Delay sequence: 50ms → 100ms → 200ms → 400ms → 800ms
Total time:     ~1.5 seconds with 25% jitter
Use in tests to avoid long waits
```

---

## HMAC Signature Pattern

### Generate Signature
```csharp
var signature = _signatureProvider.GenerateSignature(jsonPayload, webhookSecret);
// Returns: hex-encoded HMAC-SHA256
// Example: "a1b2c3d4e5f6..." (64 hex characters)
```

### Verify Signature
```csharp
bool isValid = _signatureProvider.VerifySignature(payload, receivedSignature, secret);
// Uses constant-time comparison to prevent timing attacks
```

### HTTP Headers
```
X-Signature: sha256=<hex_signature>
X-Timestamp: <unix_seconds>
X-Request-Id: <guid>
```

---

## Service Dependencies

```
IPaymentNotificationService (Orchestration)
├── IPaymentNotificationRepository (Data Access)
├── IEmailNotificationService (Email Delivery)
│   └── IEmailTemplateProvider (Template Rendering)
├── IWebhookDeliveryService (Webhook Delivery)
│   └── IWebhookSignatureProvider (HMAC Signing)
└── ILogger<PaymentNotificationService> (Structured Logging)
```

---

## Notification Status Flow

```
┌──────────────┐
│   Created    │
└──────┬───────┘
       │
       ▼
┌──────────────┐
│   Pending    │  Waiting for first delivery attempt
└──────┬───────┘
       │
       ├─── SUCCESS ──────────────► ┌──────────────┐
       │                             │ Delivered    │ (status=1)
       │                             └──────────────┘
       │
       └─── FAILURE ───┬───────► ┌──────────────┐
                       │         │   Pending    │ Retry scheduled
                       │         └──────────────┘
                       │
                       └───► (5 retries exhausted)
                              ┌──────────────┐
                              │   Failed     │ (status=2)
                              └──────────────┘
```

---

## Quick Implementation Steps

### 1. Register Services (in Startup)
```csharp
services.AddPaymentNotifications(configuration);
```

### 2. Inject and Queue
```csharp
public class PaymentReceivedHandler
{
    private readonly IPaymentNotificationService _notifications;

    public async Task Handle(PaymentReceivedEvent @event)
    {
        // Queue email notification
        await _notifications.QueueNotificationAsync(
            paymentHash: @event.PaymentHash,
            new NotificationConfiguration(
                NotificationType.Email,
                "admin@example.com"
            ));

        // Queue webhook notification
        await _notifications.QueueNotificationAsync(
            @event.PaymentHash,
            new NotificationConfiguration(
                NotificationType.Webhook,
                "https://webhook.example.com/payments",
                webhookSecret: "sk_live_xxxx"
            ));
    }
}
```

### 3. Process with Background Service
```
Automatically handles retries on schedule
No additional configuration needed
```

### 4. Manual Retry (Admin UI)
```csharp
await _notifications.RetryFailedNotificationAsync(notificationId);
```

---

## Configuration Example

**appsettings.json**
```json
{
  "Notifications": {
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "UseSsl": true,
      "Username": "${SMTP_USERNAME}",
      "Password": "${SMTP_PASSWORD}",
      "FromAddress": "noreply@payments.example.com",
      "FromName": "Lightning Payments"
    },
    "Webhooks": {
      "TimeoutSeconds": 10,
      "MaxRetries": 5,
      "InitialDelaySeconds": 2
    },
    "Email": {
      "Enabled": true,
      "DefaultAdminEmail": "admin@example.com"
    }
  }
}
```

**User Secrets (Development)**
```bash
dotnet user-secrets set "Notifications:Smtp:Username" "your-email@gmail.com"
dotnet user-secrets set "Notifications:Smtp:Password" "app-password-here"
```

---

## Database Schema

**PaymentNotifications Table**
```sql
CREATE TABLE PaymentNotifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    PaymentHash NVARCHAR(64) NOT NULL,
    Type INT NOT NULL,           -- 0=Email, 1=Webhook
    Destination NVARCHAR(500) NOT NULL,
    Status INT NOT NULL,         -- 0=Pending, 1=Delivered, 2=Failed
    AttemptCount INT NOT NULL DEFAULT 0,
    MaxRetries INT NOT NULL DEFAULT 5,
    LastAttemptAt DATETIME2 NULL,
    LastErrorMessage NVARCHAR(1000) NULL,
    FailureReason NVARCHAR(500) NULL,
    NextRetryAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL,
    DeliveredAt DATETIME2 NULL,
    CorrelationId NVARCHAR(36) NULL,
    PayloadSnapshot NVARCHAR(MAX) NULL
);

-- Indexes
CREATE INDEX idx_notification_payment_hash ON PaymentNotifications(PaymentHash);
CREATE INDEX idx_notification_status ON PaymentNotifications(Status);
CREATE INDEX idx_notification_pending_retry
    ON PaymentNotifications(Status, NextRetryAt)
    WHERE Status = 0 AND NextRetryAt <= GETUTCDATE();
CREATE INDEX idx_notification_created ON PaymentNotifications(CreatedAt);
```

---

## Exception Hierarchy

```
Exception
├── WebhookSigningException
│   └── Thrown when HMAC signing fails
├── SmtpFailedException
│   └── Thrown when SMTP delivery fails
└── NotificationException (to define)
    └── General notification failures
```

---

## Testing Checklists

### Unit Tests
- [ ] HMAC signature generation produces valid hex strings
- [ ] Signature verification returns true for correct signatures
- [ ] Signature verification returns false for tampered payloads
- [ ] Exponential backoff calculates correct delays
- [ ] Notification status transitions are correct
- [ ] Empty/null inputs throw appropriate exceptions
- [ ] Payload snapshot is captured for debugging

### Integration Tests
- [ ] Email notification can be queued and delivered successfully
- [ ] Webhook notification can be queued with HMAC signature
- [ ] Failed delivery records error message and schedules retry
- [ ] Max retries are enforced and marked as failed
- [ ] Manual retry resets notification for new attempt
- [ ] Delivery history is queryable by payment hash
- [ ] Background service picks up pending notifications
- [ ] Multiple notifications for same payment are tracked separately

### Performance Tests
- [ ] Webhook delivery completes within 10 seconds under load
- [ ] Email delivery completes within 30 seconds under load
- [ ] Background service processes 100+ pending notifications efficiently
- [ ] Database queries use configured indexes

### Security Tests
- [ ] HMAC verification uses constant-time comparison
- [ ] Signatures cannot be forged without secret
- [ ] Replayed webhooks can be detected via timestamp
- [ ] Secrets are not logged or exposed in error messages
- [ ] Connection strings use environment variables, not hardcoded

---

## Common Issues & Solutions

### Issue: "All email attempts fail with timeout"
**Solution**: Check SMTP credentials and firewall rules. Gmail requires App Passwords (2FA must be enabled).

### Issue: "Webhook retries happen too quickly"
**Solution**: Use production Polly policy, not test policy. Production has longer delays (2s, 4s, 8s, etc.).

### Issue: "Signature verification fails on webhook receiver"
**Solution**: Ensure payload is serialized identically (same casing, no extra whitespace). Use provided JsonSerializerOptions.

### Issue: "Notifications not being retried after failure"
**Solution**: Verify `NotificationRetryBackgroundService` is registered with `AddHostedService<>`. Check logs for background service startup.

### Issue: "Same notification created multiple times"
**Solution**: Ensure idempotency - use payment hash as unique constraint. Payment received event should only trigger one notification per destination.

---

## Database Migration Example

```csharp
public partial class AddPaymentNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PaymentNotifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Type = table.Column<int>(type: "int", nullable: false),
                Destination = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                AttemptCount = table.Column<int>(type: "int", nullable: false),
                MaxRetries = table.Column<int>(type: "int", nullable: false),
                LastAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CorrelationId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                PayloadSnapshot = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentNotifications", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "idx_notification_payment_hash",
            table: "PaymentNotifications",
            column: "PaymentHash");

        migrationBuilder.CreateIndex(
            name: "idx_notification_status",
            table: "PaymentNotifications",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "idx_notification_pending_retry",
            table: "PaymentNotifications",
            columns: new[] { "Status", "NextRetryAt" },
            filter: "[Status] = 0 AND [NextRetryAt] <= GETUTCDATE()");

        migrationBuilder.CreateIndex(
            name: "idx_notification_created",
            table: "PaymentNotifications",
            column: "CreatedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PaymentNotifications");
    }
}
```

---

## Key Decision Points

### Why Polly?
- **Pro**: Mature, flexible, built for .NET resilience patterns
- **Con**: Adds dependency (already in project)
- **Alternative**: Manual retry logic (not recommended - loses maturity benefits)

### Why HMAC-SHA256?
- **Pro**: Industry standard (Stripe, GitHub, Twilio use it), fast, secure
- **Con**: Requires shared secret management
- **Alternative**: JWTs (more complex, similar security)

### Why Outbox Pattern?
- **Pro**: Guarantees notifications aren't lost if app crashes
- **Con**: Adds complexity, requires separate processor
- **Alternative**: Direct delivery (risks losing notifications on crashes)

### Why Separate Email & Webhook Services?
- **Pro**: Each has different retry logic (email slower), easier to test
- **Con**: Code duplication in retry logic
- **Decision**: Duplication is acceptable for different delivery mechanisms

---

## Monitoring & Observability

### Key Metrics to Track
- **Email success rate** (target: >99%)
- **Webhook delivery latency** (p95 < 2s)
- **Retry count by failure reason** (identify patterns)
- **Failed notifications after all retries** (escalate to admin)

### Log Patterns
```
[Info] Queueing Email notification for payment abc123 to admin@example.com (CorrelationId: xyz789)
[Info] Attempting delivery of notification {id} (attempt 1/5)
[Info] Payment confirmation email sent successfully to admin@example.com
[Warn] Webhook delivery to https://webhook.example.com failed with HTTP 503, retrying in 8s
[Error] Notification {id} permanently failed: Max retries (5) exceeded
```

### Health Check
```csharp
services.AddHealthChecks()
    .AddCheck<NotificationServiceHealthCheck>("notifications");
```

---

## Production Deployment Checklist

- [ ] SMTP credentials configured in Key Vault, not appsettings
- [ ] Webhook secrets stored securely (Key Vault)
- [ ] Database backup configured for notification history
- [ ] Retention policy defined for old notifications (e.g., delete after 90 days)
- [ ] Monitoring alerts for failed notifications spike
- [ ] Manual retry capability exposed in admin UI
- [ ] Runbook created for notification delivery failures
- [ ] Load test performed for peak notification volume
- [ ] Security audit completed for HMAC implementation
- [ ] Email templates reviewed and approved
- [ ] Webhook receiver documentation provided to customers

---

**For complete implementation details, see [NOTIFICATION_SYSTEM_PATTERNS.md](./NOTIFICATION_SYSTEM_PATTERNS.md)**
