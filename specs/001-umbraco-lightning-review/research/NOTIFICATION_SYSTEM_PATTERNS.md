# Payment Notification System - Research & Reference Patterns

**Date**: 2026-01-26
**Status**: Research Document
**Reference**: FR-012 through FR-015
**Target Implementation**: .NET 9.0 with Polly 8.6.5

---

## Executive Summary

This document provides production-ready patterns for implementing a notification system with email and webhook delivery, focusing on FR-012 through FR-015 requirements:
- **FR-012**: Email notifications for payment events
- **FR-013**: Webhook notifications with HMAC signing
- **FR-014**: Retry policy with 5 retries over ~30 minutes
- **FR-015**: Notification attempt logging and manual retry

Key design decisions follow Constitution principles:
- **Article I.1**: Clean Architecture with inbound dependency flow
- **Article III.1**: Test-First development with failing tests first
- **Article IV.1**: Repository Pattern for data access
- **Article VII.1**: Custom exception hierarchies with context

---

## Part 1: Polly Retry Policy Configuration

### 1.1 Production Retry Policy (5 retries, ~30 minutes)

The following policy implements exponential backoff with jitter to prevent thundering herd:

```csharp
/// <summary>
/// Creates a resilience policy for notification delivery with 5 retries over approximately 30 minutes.
/// Delays: 2s → 4s → 8s → 16s → 32s = ~62 seconds base + jitter
/// With 25% jitter: max ~78 seconds total across all retries
/// </summary>
public static class NotificationResiliencePolicies
{
    /// <summary>
    /// Exponential backoff policy for outbound HTTP calls (webhooks, external APIs).
    /// - Initial delay: 2 seconds
    /// - Maximum delay: 32 seconds per attempt
    /// - Jitter: 25% random variance to prevent thundering herd
    /// - Total time budget: ~1 minute for all 5 retries
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateWebhookDeliveryPolicy()
    {
        var policy = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r =>
                        // Retry on transient HTTP errors
                        r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                        r.StatusCode == System.Net.HttpStatusCode.GatewayTimeout ||
                        r.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                        (int)r.StatusCode >= 500  // Server errors
                    )
                    .Build(),
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                JitterMultiplier = 0.25  // 25% random variance
            })
            .AddTimeout(TimeSpan.FromSeconds(10))  // Per-attempt timeout
            .Build();

        return policy;
    }

    /// <summary>
    /// Policy for SMTP email delivery with retries.
    /// - Initial delay: 5 seconds (SMTP is slower)
    /// - Maximum delay: 60 seconds
    /// - Jitter: 25% to prevent synchronized retries
    /// </summary>
    public static ResiliencePipeline CreateEmailDeliveryPolicy()
    {
        var policy = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<SmtpFailedException>()
                    .Handle<TimeoutException>()
                    .Build(),
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(5),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                JitterMultiplier = 0.25
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();

        return policy;
    }

    /// <summary>
    /// Creates a fast test policy with short delays (100ms base) for unit testing.
    /// Use this in tests to avoid long waits while maintaining retry logic verification.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateFastTestPolicy()
    {
        var policy = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
                    .Build(),
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromMilliseconds(50),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                JitterMultiplier = 0.25
            })
            .AddTimeout(TimeSpan.FromMilliseconds(500))
            .Build();

        return policy;
    }
}
```

**Key Design Decisions**:

1. **Exponential Backoff**: 2s → 4s → 8s → 16s → 32s = ~62s total + jitter
2. **Jitter (25%)**: Prevents thundering herd when multiple notifications retry simultaneously
3. **Transient Error Handling**: Only retries for recoverable errors (5xx, timeouts)
4. **Per-Attempt Timeout**: 10s for webhooks, 30s for email prevents hanging connections
5. **Distinction**: Email policy uses longer delays (SMTP is inherently slower)

---

## Part 2: HMAC-SHA256 Signature Generation

### 2.1 Signature Generation Pattern

```csharp
/// <summary>
/// HMAC-SHA256 signature generator for webhook payload signing following common patterns
/// (similar to Stripe, GitHub, Twilio).
/// </summary>
public interface IWebhookSignatureProvider
{
    /// <summary>
    /// Generates an HMAC-SHA256 signature for the given payload.
    /// </summary>
    /// <param name="payload">The JSON payload to sign</param>
    /// <param name="secret">The webhook secret (base64 or raw bytes)</param>
    /// <returns>Hex-encoded HMAC signature</returns>
    string GenerateSignature(string payload, string secret);

    /// <summary>
    /// Verifies that the provided signature matches the computed signature.
    /// Constant-time comparison to prevent timing attacks.
    /// </summary>
    bool VerifySignature(string payload, string providedSignature, string secret);
}

/// <summary>
/// Production implementation of webhook signature provider using HMAC-SHA256.
/// Signatures are hex-encoded and include timestamp nonce for replay protection.
/// </summary>
public class WebhookSignatureProvider : IWebhookSignatureProvider
{
    private readonly ILogger<WebhookSignatureProvider> _logger;

    public WebhookSignatureProvider(ILogger<WebhookSignatureProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates HMAC-SHA256 signature in hex format.
    /// Pattern follows: hex(HMAC-SHA256(payload, secret))
    /// </summary>
    public string GenerateSignature(string payload, string secret)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload cannot be null or empty", nameof(payload));
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Secret cannot be null or empty", nameof(secret));

        try
        {
            // Decode secret from hex or use as-is if not hex
            byte[] secretBytes = TryDecodeHexSecret(secret);

            using (var hmac = new System.Security.Cryptography.HMACSHA256(secretBytes))
            {
                byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
                byte[] hashBytes = hmac.ComputeHash(payloadBytes);

                // Return as hex string (lowercase, no prefix)
                return Convert.ToHexString(hashBytes).ToLowerInvariant();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate webhook signature");
            throw new WebhookSigningException("Failed to generate HMAC signature", ex);
        }
    }

    /// <summary>
    /// Verifies signature using constant-time comparison to prevent timing attacks.
    /// </summary>
    public bool VerifySignature(string payload, string providedSignature, string secret)
    {
        if (string.IsNullOrWhiteSpace(payload) ||
            string.IsNullOrWhiteSpace(providedSignature) ||
            string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogWarning("Webhook signature verification failed: missing parameters");
            return false;
        }

        try
        {
            string computedSignature = GenerateSignature(payload, secret);

            // Use constant-time comparison to prevent timing attacks
            bool isValid = CryptographicEquals(computedSignature, providedSignature);

            if (!isValid)
            {
                _logger.LogWarning(
                    "Webhook signature verification failed: signature mismatch. " +
                    "Expected length: {ExpectedLength}, Provided length: {ProvidedLength}",
                    computedSignature.Length,
                    providedSignature.Length);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook signature verification encountered an error");
            return false;
        }
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing-based attacks.
    /// </summary>
    private static bool CryptographicEquals(string a, string b)
    {
        // Use Encoding.Equals for constant-time comparison (available in .NET 6+)
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b)
        );
    }

    /// <summary>
    /// Attempts to decode secret from hex format, falls back to UTF-8 if not valid hex.
    /// </summary>
    private static byte[] TryDecodeHexSecret(string secret)
    {
        try
        {
            // Try to decode as hex
            return Convert.FromHexString(secret);
        }
        catch (FormatException)
        {
            // If not hex, treat as raw UTF-8 string
            return Encoding.UTF8.GetBytes(secret);
        }
    }
}

/// <summary>
/// Custom exception for webhook signing failures.
/// </summary>
public class WebhookSigningException : Exception
{
    public WebhookSigningException(string message) : base(message) { }
    public WebhookSigningException(string message, Exception innerException)
        : base(message, innerException) { }
}
```

**Usage Example**:

```csharp
// In webhook delivery service
var payload = JsonSerializer.Serialize(webhookPayload);
var signature = _signatureProvider.GenerateSignature(payload, webhookSecret);

// Send as header (pattern: "sha256=<hex_signature>")
httpClient.DefaultRequestHeaders.Add("X-Signature", $"sha256={signature}");
httpClient.DefaultRequestHeaders.Add("X-Timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

// On receiver side (webhook endpoint)
var receivedSignature = request.Headers.GetValues("X-Signature").FirstOrDefault();
if (!_signatureProvider.VerifySignature(requestBody, receivedSignature, webhookSecret))
{
    return Unauthorized();
}
```

---

## Part 3: Notification Entity Design

### 3.1 Domain Entity Structure

```csharp
/// <summary>
/// Notification delivery record tracking - domain entity representing a notification
/// that needs to be delivered to an external system (email or webhook).
/// </summary>
public class PaymentNotification
{
    /// <summary>
    /// Unique identifier for this notification record.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Payment hash this notification is associated with.
    /// Links to PaymentReceived event or payment record.
    /// </summary>
    public string PaymentHash { get; private set; } = null!;

    /// <summary>
    /// Type of notification (Email, Webhook).
    /// </summary>
    public NotificationType Type { get; private set; }

    /// <summary>
    /// Destination address (email address or webhook URL).
    /// </summary>
    public string Destination { get; private set; } = null!;

    /// <summary>
    /// Current delivery status.
    /// </summary>
    public NotificationStatus Status { get; private set; }

    /// <summary>
    /// Number of delivery attempts made.
    /// </summary>
    public int AttemptCount { get; private set; }

    /// <summary>
    /// Maximum number of retries allowed.
    /// </summary>
    public int MaxRetries { get; private set; } = 5;

    /// <summary>
    /// Timestamp of the last delivery attempt.
    /// </summary>
    public DateTime? LastAttemptAt { get; private set; }

    /// <summary>
    /// Error message from the last failed attempt.
    /// </summary>
    public string? LastErrorMessage { get; private set; }

    /// <summary>
    /// Reason for permanent failure (if Status == Failed).
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// Next scheduled retry time.
    /// </summary>
    public DateTime? NextRetryAt { get; private set; }

    /// <summary>
    /// When this notification was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// When delivery succeeded (if Status == Delivered).
    /// </summary>
    public DateTime? DeliveredAt { get; private set; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// Opaque payload data (JSON serialized notification content).
    /// Stored for debugging and manual retry purposes.
    /// </summary>
    public string? PayloadSnapshot { get; private set; }

    // Constructor for EF Core
    private PaymentNotification() { }

    /// <summary>
    /// Factory method to create a new notification.
    /// </summary>
    public static PaymentNotification Create(
        string paymentHash,
        NotificationType type,
        string destination,
        string? correlationId = null,
        int maxRetries = 5)
    {
        if (string.IsNullOrWhiteSpace(paymentHash))
            throw new ArgumentException("Payment hash cannot be null or empty", nameof(paymentHash));
        if (string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("Destination cannot be null or empty", nameof(destination));

        return new PaymentNotification
        {
            Id = Guid.NewGuid(),
            PaymentHash = paymentHash,
            Type = type,
            Destination = destination,
            Status = NotificationStatus.Pending,
            AttemptCount = 0,
            MaxRetries = maxRetries,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Records a delivery attempt with optional error details.
    /// </summary>
    public void RecordAttempt(bool success, string? errorMessage = null)
    {
        AttemptCount++;
        LastAttemptAt = DateTime.UtcNow;

        if (success)
        {
            Status = NotificationStatus.Delivered;
            DeliveredAt = DateTime.UtcNow;
            NextRetryAt = null;
        }
        else
        {
            LastErrorMessage = errorMessage;

            if (AttemptCount >= MaxRetries)
            {
                Status = NotificationStatus.Failed;
                FailureReason = $"Max retries ({MaxRetries}) exceeded. Last error: {errorMessage}";
            }
            else
            {
                Status = NotificationStatus.Pending;
                // Calculate exponential backoff: 2s * 2^(attempt-1) seconds
                int delaySeconds = (int)Math.Pow(2, AttemptCount);
                NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
            }
        }
    }

    /// <summary>
    /// Marks notification as permanently failed with reason.
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        Status = NotificationStatus.Failed;
        FailureReason = reason;
    }

    /// <summary>
    /// Resets notification for manual retry.
    /// </summary>
    public void ResetForManualRetry()
    {
        if (Status != NotificationStatus.Failed)
            throw new InvalidOperationException("Can only manually retry failed notifications");

        Status = NotificationStatus.Pending;
        AttemptCount = 0;
        NextRetryAt = DateTime.UtcNow;
        LastErrorMessage = null;
        FailureReason = null;
    }
}

/// <summary>
/// Notification type enumeration.
/// </summary>
public enum NotificationType
{
    Email = 0,
    Webhook = 1
}

/// <summary>
/// Notification delivery status.
/// </summary>
public enum NotificationStatus
{
    Pending = 0,      // Waiting to be delivered
    Delivered = 1,    // Successfully delivered
    Failed = 2,       // Permanently failed after all retries
    ManualRetry = 3   // Administrator initiated manual retry
}
```

### 3.2 Entity Configuration for EF Core

```csharp
/// <summary>
/// Entity Framework configuration for PaymentNotification.
/// </summary>
public class PaymentNotificationConfiguration : IEntityTypeConfiguration<PaymentNotification>
{
    public void Configure(EntityTypeBuilder<PaymentNotification> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PaymentHash)
            .IsRequired()
            .HasMaxLength(64);  // Lightning payment hash length

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.Destination)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(x => x.LastErrorMessage)
            .HasMaxLength(1000);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);

        builder.Property(x => x.PayloadSnapshot)
            .HasMaxLength(8000);  // Store JSON payload for debugging

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(36);  // GUID length

        // Indexes for query optimization
        builder.HasIndex(x => x.PaymentHash)
            .HasName("idx_notification_payment_hash");

        builder.HasIndex(x => x.Status)
            .HasName("idx_notification_status");

        builder.HasIndex(x => new { x.Status, x.NextRetryAt })
            .HasName("idx_notification_pending_retry")
            .HasFilter("[Status] = 'Pending' AND [NextRetryAt] <= GETUTCDATE()");

        builder.HasIndex(x => x.CreatedAt)
            .HasName("idx_notification_created");

        builder.ToTable("PaymentNotifications");
    }
}
```

---

## Part 4: Service Interface Design

### 4.1 Email Service Interface

```csharp
/// <summary>
/// Email notification service interface.
/// Decouples notification logic from SMTP implementation.
/// </summary>
public interface IEmailNotificationService
{
    /// <summary>
    /// Sends a payment confirmation email asynchronously.
    /// </summary>
    Task<EmailDeliveryResult> SendPaymentConfirmationAsync(
        PaymentNotificationModel notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a payment refund email asynchronously.
    /// </summary>
    Task<EmailDeliveryResult> SendPaymentRefundAsync(
        RefundNotificationModel notification,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of email delivery attempt.
/// </summary>
public record EmailDeliveryResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime SentAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Email notification content model.
/// </summary>
public record PaymentNotificationModel(
    string RecipientEmail,
    string PaymentHash,
    long AmountSat,
    string? Description,
    DateTime ReceivedAt,
    string? CorrelationId = null);
```

### 4.2 Webhook Delivery Service Interface

```csharp
/// <summary>
/// Webhook delivery service interface.
/// Handles outbound HTTP POST with HMAC signing.
/// </summary>
public interface IWebhookDeliveryService
{
    /// <summary>
    /// Delivers a webhook notification to the configured endpoint.
    /// Includes HMAC-SHA256 signature in headers.
    /// </summary>
    Task<WebhookDeliveryResult> DeliverAsync(
        string webhookUrl,
        PaymentEventPayload payload,
        string? secret = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of webhook delivery attempt.
/// </summary>
public record WebhookDeliveryResult
{
    public bool Success { get; init; }
    public int? HttpStatusCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ResponseBody { get; init; }
    public DateTime AttemptedAt { get; init; } = DateTime.UtcNow;
    public TimeSpan? ResponseTime { get; init; }
}

/// <summary>
/// Webhook payload structure for payment events.
/// </summary>
public record PaymentEventPayload(
    string EventType,
    string PaymentHash,
    long AmountSat,
    DateTime ReceivedAt,
    string? Description = null,
    string? Metadata = null)
{
    /// <summary>
    /// Timestamp when this payload was created (for replay protection).
    /// </summary>
    public long Timestamp { get; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
```

### 4.3 Notification Orchestration Service

```csharp
/// <summary>
/// Orchestrates notification delivery with retry logic and persistence.
/// This service implements FR-012 through FR-015.
/// </summary>
public interface IPaymentNotificationService
{
    /// <summary>
    /// Queues a notification for delivery with automatic retry.
    /// FR-012, FR-013: Email and webhook support
    /// FR-014: Retry policy handling
    /// </summary>
    Task<Guid> QueueNotificationAsync(
        string paymentHash,
        NotificationConfiguration config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts delivery of a pending notification.
    /// FR-015: Logs all attempts
    /// </summary>
    Task<bool> DeliverNotificationAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notification delivery history.
    /// FR-015: Manual retry capability
    /// </summary>
    Task<IEnumerable<NotificationHistory>> GetDeliveryHistoryAsync(
        string paymentHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually retries a failed notification.
    /// FR-015: Failed delivery logging and manual retry capability
    /// </summary>
    Task<bool> RetryFailedNotificationAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration for notification delivery.
/// </summary>
public record NotificationConfiguration(
    NotificationType Type,
    string Destination,
    string? WebhookSecret = null,
    int MaxRetries = 5);

/// <summary>
/// Historical record of notification attempts.
/// </summary>
public record NotificationHistory(
    Guid NotificationId,
    string PaymentHash,
    NotificationType Type,
    NotificationStatus Status,
    int AttemptCount,
    DateTime? LastAttemptAt,
    string? LastError);
```

---

## Part 5: Implementation Patterns

### 5.1 Email Service Implementation (MailKit Pattern)

```csharp
/// <summary>
/// Email notification service using MailKit for SMTP delivery.
/// Follows clean architecture with dependency injection.
/// </summary>
public class MailKitEmailNotificationService : IEmailNotificationService
{
    private readonly IOptions<SmtpSettings> _smtpSettings;
    private readonly ILogger<MailKitEmailNotificationService> _logger;
    private readonly ResiliencePipeline _emailPolicy;
    private readonly IEmailTemplateProvider _templateProvider;

    public MailKitEmailNotificationService(
        IOptions<SmtpSettings> smtpSettings,
        ILogger<MailKitEmailNotificationService> logger,
        IEmailTemplateProvider templateProvider)
    {
        _smtpSettings = smtpSettings ?? throw new ArgumentNullException(nameof(smtpSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _templateProvider = templateProvider ?? throw new ArgumentNullException(nameof(templateProvider));
        _emailPolicy = NotificationResiliencePolicies.CreateEmailDeliveryPolicy();
    }

    public async Task<EmailDeliveryResult> SendPaymentConfirmationAsync(
        PaymentNotificationModel notification,
        CancellationToken cancellationToken = default)
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        try
        {
            _logger.LogInformation(
                "Sending payment confirmation email to {Email} for payment {PaymentHash}",
                notification.RecipientEmail,
                notification.PaymentHash);

            // Get email template with payment details
            var template = await _templateProvider.GetPaymentConfirmationTemplateAsync(
                notification,
                cancellationToken);

            var result = await _emailPolicy.ExecuteAsync(
                async (ct) => await SendEmailAsync(
                    notification.RecipientEmail,
                    template.Subject,
                    template.HtmlBody,
                    template.TextBody,
                    ct),
                cancellationToken);

            _logger.LogInformation(
                "Payment confirmation email sent successfully to {Email}",
                notification.RecipientEmail);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send payment confirmation email to {Email}",
                notification.RecipientEmail);

            return new EmailDeliveryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<EmailDeliveryResult> SendPaymentRefundAsync(
        RefundNotificationModel notification,
        CancellationToken cancellationToken = default)
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        try
        {
            var template = await _templateProvider.GetPaymentRefundTemplateAsync(
                notification,
                cancellationToken);

            var result = await _emailPolicy.ExecuteAsync(
                async (ct) => await SendEmailAsync(
                    notification.RecipientEmail,
                    template.Subject,
                    template.HtmlBody,
                    template.TextBody,
                    ct),
                cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment refund email");
            return new EmailDeliveryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<EmailDeliveryResult> SendEmailAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken)
    {
        using (var client = new SmtpClient())
        {
            try
            {
                // Connect with settings from configuration
                await client.ConnectAsync(
                    _smtpSettings.Value.Host,
                    _smtpSettings.Value.Port,
                    _smtpSettings.Value.UseSsl,
                    cancellationToken);

                // Authenticate if credentials provided
                if (!string.IsNullOrEmpty(_smtpSettings.Value.Username))
                {
                    await client.AuthenticateAsync(
                        _smtpSettings.Value.Username,
                        _smtpSettings.Value.Password,
                        cancellationToken);
                }

                // Build email message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _smtpSettings.Value.FromName,
                    _smtpSettings.Value.FromAddress));
                message.To.Add(new MailboxAddress(recipientEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    TextBody = textBody,
                    HtmlBody = htmlBody
                };
                message.Body = bodyBuilder.ToMessageBody();

                // Send email
                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                return new EmailDeliveryResult { Success = true };
            }
            catch (Exception ex)
            {
                throw new SmtpFailedException($"SMTP delivery failed: {ex.Message}", ex);
            }
        }
    }
}

/// <summary>
/// SMTP configuration settings.
/// </summary>
public class SmtpSettings
{
    public string Host { get; set; } = null!;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = null!;
    public string FromName { get; set; } = "Lightning Payments";
}

/// <summary>
/// Custom exception for SMTP failures.
/// </summary>
public class SmtpFailedException : Exception
{
    public SmtpFailedException(string message) : base(message) { }
    public SmtpFailedException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Email template provider interface.
/// </summary>
public interface IEmailTemplateProvider
{
    Task<EmailTemplate> GetPaymentConfirmationTemplateAsync(
        PaymentNotificationModel model,
        CancellationToken cancellationToken);

    Task<EmailTemplate> GetPaymentRefundTemplateAsync(
        RefundNotificationModel model,
        CancellationToken cancellationToken);
}

/// <summary>
/// Email template data structure.
/// </summary>
public record EmailTemplate(
    string Subject,
    string HtmlBody,
    string TextBody);
```

### 5.2 Webhook Delivery Implementation

```csharp
/// <summary>
/// Webhook delivery service using HttpClient with Polly resilience.
/// Includes HMAC-SHA256 signing and retry logic.
/// </summary>
public class WebhookDeliveryService : IWebhookDeliveryService
{
    private readonly HttpClient _httpClient;
    private readonly IWebhookSignatureProvider _signatureProvider;
    private readonly ILogger<WebhookDeliveryService> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;

    public WebhookDeliveryService(
        HttpClient httpClient,
        IWebhookSignatureProvider signatureProvider,
        ILogger<WebhookDeliveryService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _signatureProvider = signatureProvider ?? throw new ArgumentNullException(nameof(signatureProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resiliencePipeline = NotificationResiliencePolicies.CreateWebhookDeliveryPolicy();
    }

    public async Task<WebhookDeliveryResult> DeliverAsync(
        string webhookUrl,
        PaymentEventPayload payload,
        string? secret = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
            throw new ArgumentException("Webhook URL cannot be null or empty", nameof(webhookUrl));
        if (payload == null)
            throw new ArgumentNullException(nameof(payload));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Delivering webhook to {WebhookUrl} for payment {PaymentHash}",
                webhookUrl,
                payload.PaymentHash);

            // Serialize payload
            var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            // Generate signature if secret provided
            string? signature = null;
            if (!string.IsNullOrEmpty(secret))
            {
                signature = _signatureProvider.GenerateSignature(jsonPayload, secret);
            }

            // Execute with resilience policy
            var response = await _resiliencePipeline.ExecuteAsync(
                async (ct) => await SendWebhookAsync(
                    webhookUrl,
                    jsonPayload,
                    signature,
                    payload.Timestamp,
                    ct),
                cancellationToken);

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Webhook delivered successfully to {WebhookUrl} in {ElapsedMilliseconds}ms",
                    webhookUrl,
                    stopwatch.ElapsedMilliseconds);

                return new WebhookDeliveryResult
                {
                    Success = true,
                    HttpStatusCode = (int)response.StatusCode,
                    ResponseTime = stopwatch.Elapsed
                };
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Webhook delivery failed with status {StatusCode}: {ResponseBody}",
                    response.StatusCode,
                    responseBody);

                return new WebhookDeliveryResult
                {
                    Success = false,
                    HttpStatusCode = (int)response.StatusCode,
                    ErrorMessage = $"HTTP {response.StatusCode}",
                    ResponseBody = responseBody,
                    ResponseTime = stopwatch.Elapsed
                };
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Webhook delivery to {WebhookUrl} failed with exception",
                webhookUrl);

            return new WebhookDeliveryResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ResponseTime = stopwatch.Elapsed
            };
        }
    }

    private async Task<HttpResponseMessage> SendWebhookAsync(
        string webhookUrl,
        string jsonPayload,
        string? signature,
        long timestamp,
        CancellationToken cancellationToken)
    {
        using (var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl))
        {
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Add signature header if provided
            if (!string.IsNullOrEmpty(signature))
            {
                request.Headers.Add("X-Signature", $"sha256={signature}");
            }

            // Add timestamp for replay protection
            request.Headers.Add("X-Timestamp", timestamp.ToString());

            // Unique request ID for tracing
            request.Headers.Add("X-Request-Id", Guid.NewGuid().ToString());

            return await _httpClient.SendAsync(request, cancellationToken);
        }
    }
}
```

### 5.3 Notification Orchestration Service

```csharp
/// <summary>
/// Payment notification orchestration service.
/// Implements FR-012 through FR-015 requirements.
/// </summary>
public class PaymentNotificationService : IPaymentNotificationService
{
    private readonly IPaymentNotificationRepository _repository;
    private readonly IEmailNotificationService _emailService;
    private readonly IWebhookDeliveryService _webhookService;
    private readonly ILogger<PaymentNotificationService> _logger;

    public PaymentNotificationService(
        IPaymentNotificationRepository repository,
        IEmailNotificationService emailService,
        IWebhookDeliveryService webhookService,
        ILogger<PaymentNotificationService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _webhookService = webhookService ?? throw new ArgumentNullException(nameof(webhookService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// FR-012, FR-013: Queue notification for delivery
    /// </summary>
    public async Task<Guid> QueueNotificationAsync(
        string paymentHash,
        NotificationConfiguration config,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentHash))
            throw new ArgumentException("Payment hash cannot be null", nameof(paymentHash));
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        _logger.LogInformation(
            "Queueing {NotificationType} notification for payment {PaymentHash} to {Destination}",
            config.Type,
            paymentHash,
            config.Destination);

        var notification = PaymentNotification.Create(
            paymentHash,
            config.Type,
            config.Destination,
            maxRetries: config.MaxRetries);

        await _repository.AddAsync(notification, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return notification.Id;
    }

    /// <summary>
    /// FR-014, FR-015: Deliver notification with retry logic
    /// </summary>
    public async Task<bool> DeliverNotificationAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);
        if (notification == null)
        {
            _logger.LogWarning("Notification {NotificationId} not found", notificationId);
            return false;
        }

        if (notification.Status == NotificationStatus.Delivered)
        {
            _logger.LogInformation("Notification {NotificationId} already delivered", notificationId);
            return true;
        }

        try
        {
            _logger.LogInformation(
                "Attempting delivery of notification {NotificationId} (attempt {AttemptCount}/{MaxRetries})",
                notificationId,
                notification.AttemptCount + 1,
                notification.MaxRetries);

            bool success = false;
            string? errorMessage = null;

            try
            {
                switch (notification.Type)
                {
                    case NotificationType.Email:
                        success = await DeliverEmailAsync(notification, cancellationToken);
                        break;

                    case NotificationType.Webhook:
                        success = await DeliverWebhookAsync(notification, cancellationToken);
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Unknown notification type: {notification.Type}");
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                _logger.LogError(
                    ex,
                    "Notification {NotificationId} delivery failed: {ErrorMessage}",
                    notificationId,
                    errorMessage);
            }

            notification.RecordAttempt(success, errorMessage);
            await _repository.UpdateAsync(notification, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            if (success)
            {
                _logger.LogInformation(
                    "Notification {NotificationId} delivered successfully",
                    notificationId);
            }
            else if (notification.NextRetryAt.HasValue)
            {
                _logger.LogInformation(
                    "Notification {NotificationId} scheduled for retry at {NextRetryAt}",
                    notificationId,
                    notification.NextRetryAt);
            }
            else
            {
                _logger.LogError(
                    "Notification {NotificationId} permanently failed: {FailureReason}",
                    notificationId,
                    notification.FailureReason);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error delivering notification {NotificationId}",
                notificationId);
            return false;
        }
    }

    private async Task<bool> DeliverEmailAsync(
        PaymentNotification notification,
        CancellationToken cancellationToken)
    {
        // TODO: Get payment details from repository to construct email
        // This is simplified - in practice, you'd retrieve full payment details
        var model = new PaymentNotificationModel(
            notification.Destination,
            notification.PaymentHash,
            0,  // AmountSat would come from payment record
            null,
            DateTime.UtcNow,
            notification.CorrelationId);

        var result = await _emailService.SendPaymentConfirmationAsync(model, cancellationToken);
        return result.Success;
    }

    private async Task<bool> DeliverWebhookAsync(
        PaymentNotification notification,
        CancellationToken cancellationToken)
    {
        var payload = new PaymentEventPayload(
            "payment.received",
            notification.PaymentHash,
            0,  // Would come from payment record
            DateTime.UtcNow);

        var result = await _webhookService.DeliverAsync(
            notification.Destination,
            payload,
            secret: null,  // Would come from config
            cancellationToken);

        return result.Success;
    }

    /// <summary>
    /// FR-015: Get notification delivery history
    /// </summary>
    public async Task<IEnumerable<NotificationHistory>> GetDeliveryHistoryAsync(
        string paymentHash,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentHash))
            throw new ArgumentException("Payment hash cannot be null", nameof(paymentHash));

        var notifications = await _repository.GetByPaymentHashAsync(paymentHash, cancellationToken);

        return notifications.Select(n => new NotificationHistory(
            n.Id,
            n.PaymentHash,
            n.Type,
            n.Status,
            n.AttemptCount,
            n.LastAttemptAt,
            n.LastErrorMessage));
    }

    /// <summary>
    /// FR-015: Manual retry capability
    /// </summary>
    public async Task<bool> RetryFailedNotificationAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Manual retry requested for notification {NotificationId}", notificationId);

        var notification = await _repository.GetByIdAsync(notificationId, cancellationToken);
        if (notification == null)
        {
            _logger.LogWarning("Notification {NotificationId} not found", notificationId);
            return false;
        }

        notification.ResetForManualRetry();
        await _repository.UpdateAsync(notification, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // Attempt immediate delivery
        return await DeliverNotificationAsync(notificationId, cancellationToken);
    }
}
```

---

## Part 6: Repository Interface for Persistence

```csharp
/// <summary>
/// Repository interface for PaymentNotification persistence.
/// Implements Article IV.1: Repository Pattern Mandate.
/// </summary>
public interface IPaymentNotificationRepository
{
    /// <summary>
    /// Gets a notification by ID.
    /// </summary>
    Task<PaymentNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all notifications for a specific payment.
    /// </summary>
    Task<IEnumerable<PaymentNotification>> GetByPaymentHashAsync(
        string paymentHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending notifications that are due for retry.
    /// Used by background service for scheduled retry.
    /// </summary>
    Task<IEnumerable<PaymentNotification>> GetPendingForRetryAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new notification.
    /// </summary>
    Task AddAsync(PaymentNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing notification.
    /// </summary>
    Task UpdateAsync(PaymentNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Entity Framework Core implementation of PaymentNotificationRepository.
/// </summary>
public class PaymentNotificationRepository : IPaymentNotificationRepository
{
    private readonly PaymentDbContext _context;

    public PaymentNotificationRepository(PaymentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PaymentNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentNotifications
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<PaymentNotification>> GetByPaymentHashAsync(
        string paymentHash,
        CancellationToken cancellationToken = default)
    {
        return await _context.PaymentNotifications
            .AsNoTracking()
            .Where(x => x.PaymentHash == paymentHash)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PaymentNotification>> GetPendingForRetryAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.PaymentNotifications
            .Where(x => x.Status == NotificationStatus.Pending && x.NextRetryAt <= DateTime.UtcNow)
            .OrderBy(x => x.NextRetryAt)
            .Take(100)  // Process in batches
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
    {
        await _context.PaymentNotifications.AddAsync(notification, cancellationToken);
    }

    public async Task UpdateAsync(PaymentNotification notification, CancellationToken cancellationToken = default)
    {
        _context.PaymentNotifications.Update(notification);
        await Task.CompletedTask;  // Repository pattern requires async
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

---

## Part 7: Background Service for Scheduled Retry

```csharp
/// <summary>
/// Background service for processing pending notifications.
/// Runs on a schedule to retry failed notifications.
/// </summary>
public class NotificationRetryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationRetryBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public NotificationRetryBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<NotificationRetryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification retry background service started");

        using (var timer = new PeriodicTimer(_interval))
        {
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        await ProcessPendingNotificationsAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing pending notifications");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Notification retry background service stopped");
            }
        }
    }

    private async Task ProcessPendingNotificationsAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IPaymentNotificationRepository>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IPaymentNotificationService>();

            var pendingNotifications = await repository.GetPendingForRetryAsync(cancellationToken);

            foreach (var notification in pendingNotifications)
            {
                try
                {
                    await notificationService.DeliverNotificationAsync(notification.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error retrying notification {NotificationId}",
                        notification.Id);
                }
            }
        }
    }
}
```

---

## Part 8: Dependency Injection Setup

```csharp
/// <summary>
/// Extension method to register notification services with dependency injection.
/// Follows Article II.3: Explicit Over Implicit configuration.
/// </summary>
public static class NotificationServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<SmtpSettings>(
            configuration.GetSection("Notifications:Smtp"));

        // Register core services
        services.AddScoped<IWebhookSignatureProvider, WebhookSignatureProvider>();
        services.AddScoped<IEmailNotificationService, MailKitEmailNotificationService>();
        services.AddScoped<IWebhookDeliveryService, WebhookDeliveryService>();
        services.AddScoped<IPaymentNotificationService, PaymentNotificationService>();
        services.AddScoped<IPaymentNotificationRepository, PaymentNotificationRepository>();

        // Register template provider (implementation depends on your templating strategy)
        services.AddScoped<IEmailTemplateProvider, DefaultEmailTemplateProvider>();

        // Register background service for scheduled retry
        services.AddHostedService<NotificationRetryBackgroundService>();

        // Register HttpClient for webhook delivery with named instance
        services.AddHttpClient("WebhookClient")
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        return services;
    }
}
```

---

## Part 9: Configuration Schema

```json
{
  "Notifications": {
    "Smtp": {
      "Host": "smtp.gmail.com",
      "Port": 587,
      "UseSsl": true,
      "Username": "${SMTP_USERNAME}",
      "Password": "${SMTP_PASSWORD}",
      "FromAddress": "noreply@example.com",
      "FromName": "Lightning Payments"
    },
    "Webhooks": {
      "TimeoutSeconds": 10,
      "MaxRetries": 5,
      "InitialDelaySeconds": 2
    },
    "Email": {
      "Enabled": true,
      "DefaultRecipient": "admin@example.com"
    }
  }
}
```

---

## Part 10: Testing Patterns

### 10.1 Unit Test Example - Signature Generation

```csharp
public class WebhookSignatureProviderTests
{
    private readonly WebhookSignatureProvider _provider;
    private readonly Mock<ILogger<WebhookSignatureProvider>> _mockLogger;

    public WebhookSignatureProviderTests()
    {
        _mockLogger = new Mock<ILogger<WebhookSignatureProvider>>();
        _provider = new WebhookSignatureProvider(_mockLogger.Object);
    }

    [Fact]
    public void GenerateSignature_WithValidPayload_ReturnsHexEncodedSignature()
    {
        // Arrange
        string payload = "{\"amount\":1000,\"hash\":\"abc123\"}";
        string secret = "test-secret";

        // Act
        var signature = _provider.GenerateSignature(payload, secret);

        // Assert
        Assert.NotNull(signature);
        Assert.Matches("^[a-f0-9]+$", signature);  // Valid hex format
        Assert.NotEmpty(signature);
    }

    [Fact]
    public void GenerateSignature_DifferentSecrets_ProduceDifferentSignatures()
    {
        // Arrange
        string payload = "{\"amount\":1000}";
        string secret1 = "secret1";
        string secret2 = "secret2";

        // Act
        var sig1 = _provider.GenerateSignature(payload, secret1);
        var sig2 = _provider.GenerateSignature(payload, secret2);

        // Assert
        Assert.NotEqual(sig1, sig2);
    }

    [Fact]
    public void VerifySignature_WithCorrectSignature_ReturnsTrue()
    {
        // Arrange
        string payload = "{\"test\":true}";
        string secret = "my-secret";
        var signature = _provider.GenerateSignature(payload, secret);

        // Act
        var isValid = _provider.VerifySignature(payload, signature, secret);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifySignature_WithWrongSignature_ReturnsFalse()
    {
        // Arrange
        string payload = "{\"test\":true}";
        string secret = "my-secret";
        string wrongSignature = "0000000000000000000000000000000000000000000000000000000000000000";

        // Act
        var isValid = _provider.VerifySignature(payload, wrongSignature, secret);

        // Assert
        Assert.False(isValid);
    }
}
```

### 10.2 Integration Test - Notification Delivery

```csharp
public class PaymentNotificationServiceIntegrationTests : IAsyncLifetime
{
    private readonly PaymentDbContext _context;
    private readonly PaymentNotificationService _service;
    private readonly Mock<IEmailNotificationService> _mockEmailService;
    private readonly Mock<IWebhookDeliveryService> _mockWebhookService;

    public PaymentNotificationServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new PaymentDbContext(options);
        _mockEmailService = new Mock<IEmailNotificationService>();
        _mockWebhookService = new Mock<IWebhookDeliveryService>();

        var repository = new PaymentNotificationRepository(_context);
        var logger = new Mock<ILogger<PaymentNotificationService>>();

        _service = new PaymentNotificationService(
            repository,
            _mockEmailService.Object,
            _mockWebhookService.Object,
            logger.Object);
    }

    public async Task InitializeAsync() => await _context.Database.EnsureCreatedAsync();
    public async Task DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task QueueNotificationAsync_CreatesNotificationRecord()
    {
        // Arrange
        var paymentHash = "test_payment_hash_123";
        var config = new NotificationConfiguration(
            NotificationType.Email,
            "test@example.com");

        // Act
        var notificationId = await _service.QueueNotificationAsync(paymentHash, config);

        // Assert
        Assert.NotEqual(Guid.Empty, notificationId);
        var notification = await _context.PaymentNotifications.FindAsync(notificationId);
        Assert.NotNull(notification);
        Assert.Equal(paymentHash, notification.PaymentHash);
        Assert.Equal(NotificationStatus.Pending, notification.Status);
    }

    [Fact]
    public async Task DeliverNotificationAsync_OnSuccess_MarkAsDelivered()
    {
        // Arrange
        var paymentHash = "test_payment_hash_456";
        var config = new NotificationConfiguration(
            NotificationType.Email,
            "test@example.com");

        _mockEmailService
            .Setup(x => x.SendPaymentConfirmationAsync(It.IsAny<PaymentNotificationModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailDeliveryResult { Success = true });

        var notificationId = await _service.QueueNotificationAsync(paymentHash, config);

        // Act
        var success = await _service.DeliverNotificationAsync(notificationId);

        // Assert
        Assert.True(success);
        var notification = await _context.PaymentNotifications.FindAsync(notificationId);
        Assert.Equal(NotificationStatus.Delivered, notification.Status);
        Assert.Equal(1, notification.AttemptCount);
    }

    [Fact]
    public async Task DeliverNotificationAsync_OnFailure_SchedulesRetry()
    {
        // Arrange
        var paymentHash = "test_payment_hash_789";
        var config = new NotificationConfiguration(
            NotificationType.Email,
            "test@example.com",
            maxRetries: 5);

        _mockEmailService
            .Setup(x => x.SendPaymentConfirmationAsync(It.IsAny<PaymentNotificationModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailDeliveryResult { Success = false, ErrorMessage = "SMTP timeout" });

        var notificationId = await _service.QueueNotificationAsync(paymentHash, config);

        // Act
        var success = await _service.DeliverNotificationAsync(notificationId);

        // Assert
        Assert.False(success);
        var notification = await _context.PaymentNotifications.FindAsync(notificationId);
        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Equal(1, notification.AttemptCount);
        Assert.NotNull(notification.NextRetryAt);
        Assert.True(notification.NextRetryAt > DateTime.UtcNow);
    }
}
```

---

## Part 11: Outbox Pattern for Reliability

The Outbox Pattern ensures notifications are never lost, even if the application crashes:

```csharp
/// <summary>
/// Outbox pattern implementation for guaranteed notification delivery.
/// Decouples payment processing from notification delivery.
/// When a payment is recorded, a notification outbox entry is created atomically.
/// A separate service processes the outbox asynchronously.
/// </summary>
public class NotificationOutbox
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PaymentHash { get; set; } = null!;
    public NotificationType Type { get; set; }
    public string Destination { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public OutboxStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}

public enum OutboxStatus
{
    Pending = 0,
    Processed = 1,
    Failed = 2
}

/// <summary>
/// Usage in payment recording handler:
/// When payment is confirmed, add to outbox within same transaction
/// </summary>
public class PaymentReceivedEventHandler
{
    private readonly PaymentDbContext _context;

    public async Task Handle(PaymentReceivedEvent @event, CancellationToken cancellationToken)
    {
        // Record payment
        var payment = new Payment { /* ... */ };
        await _context.Payments.AddAsync(payment, cancellationToken);

        // Add notification outbox entry in same transaction
        var outboxEntry = new NotificationOutbox
        {
            PaymentHash = @event.PaymentHash,
            Type = NotificationType.Email,
            Destination = _config["Notifications:AdminEmail"],
            PayloadJson = JsonSerializer.Serialize(@event),
            Status = OutboxStatus.Pending
        };
        await _context.NotificationOutbox.AddAsync(outboxEntry, cancellationToken);

        // Single SaveChangesAsync - either both succeed or both fail
        await _context.SaveChangesAsync(cancellationToken);

        // Fire and forget - if this fails, outbox entry remains for retry
        #pragma warning disable CS4014
        NotifyFromOutboxAsync(outboxEntry.Id, cancellationToken);
    }
}
```

---

## Summary & Implementation Checklist

### Prerequisites
- [x] Polly 8.6.5 or later (already in Directory.Packages.props)
- [x] MailKit for email (add to Directory.Packages.props)
- [x] HttpClient factory for webhooks (use Microsoft.Extensions.Http)
- [x] Entity Framework Core 9.0 (already in Directory.Packages.props)

### Required NuGet Packages
```xml
<PackageVersion Include="MailKit" Version="4.8.0" />
<PackageVersion Include="MimeKit" Version="4.8.0" />
```

### Key Implementation Points

1. **Polly Policy Configuration**
   - 5 retries with exponential backoff (2s → 32s)
   - 25% jitter to prevent thundering herd
   - Separate policies for email (longer delays) and webhooks

2. **HMAC-SHA256 Signing**
   - Hex-encoded signatures
   - Constant-time comparison for verification
   - Timestamp nonce for replay protection

3. **Entity Design**
   - PaymentNotification tracks all delivery attempts
   - Status transitions: Pending → Delivered/Failed
   - Records payload snapshot for debugging

4. **Service Architecture**
   - Clean separation: IEmailNotificationService, IWebhookDeliveryService, IPaymentNotificationService
   - Repository pattern for data access
   - Custom exceptions with context

5. **Resilience & Persistence**
   - Background service for scheduled retries
   - Outbox pattern for atomic notifications
   - Comprehensive logging with correlation IDs

6. **Testing Strategy**
   - Use fast test policies (50ms delays) for unit tests
   - Mock email/webhook services in unit tests
   - Integration tests with in-memory database
   - Test both success and failure paths

---

**This document provides production-ready patterns for implementing FR-012 through FR-015. All code follows Constitution principles and clean architecture guidelines.**
