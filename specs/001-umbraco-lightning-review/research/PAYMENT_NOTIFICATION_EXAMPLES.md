# Payment Notification System - BreezSDK Implementation Examples

**Context**: Lightning payments via BreezSDK in Umbraco
**References**: FR-012 through FR-015

---

## Part 1: Integration with Payment Received Event

### Event Model
```csharp
/// <summary>
/// Domain event raised when a Lightning payment is received and confirmed.
/// Published by BreezSDK payment listener.
/// </summary>
public record PaymentReceivedEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string PaymentHash { get; init; } = null!;  // Payment hash from invoice
    public long AmountSat { get; init; }               // Amount in satoshis
    public string? Description { get; init; }          // Invoice description
    public DateTime ReceivedAt { get; init; }

    public string? PayeeEmail { get; init; }           // Optional: who to notify
    public string? WebhookUrl { get; init; }           // Optional: webhook destination
    public string? WebhookSecret { get; init; }        // Optional: webhook signing secret

    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();
}
```

### Event Handler (Notification Trigger)
```csharp
/// <summary>
/// Domain event handler that triggers payment notifications
/// when a payment is received via BreezSDK.
/// Implements FR-012 through FR-015.
/// </summary>
public class PaymentReceivedEventHandler
{
    private readonly IPaymentNotificationService _notificationService;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentReceivedEventHandler> _logger;

    public PaymentReceivedEventHandler(
        IPaymentNotificationService notificationService,
        IConfiguration config,
        ILogger<PaymentReceivedEventHandler> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles payment received event by queuing notifications.
    /// Implements FR-012 (email) and FR-013 (webhook).
    /// </summary>
    public async Task Handle(PaymentReceivedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Payment received: {PaymentHash}, Amount: {AmountSat} sats, " +
            "CorrelationId: {CorrelationId}",
            @event.PaymentHash,
            @event.AmountSat,
            @event.CorrelationId);

        try
        {
            // FR-012: Queue email notification to admin
            await QueueAdminEmailAsync(@event, cancellationToken);

            // FR-012: Queue email notification to payee (if configured)
            if (!string.IsNullOrEmpty(@event.PayeeEmail))
            {
                await QueuePayeeEmailAsync(@event, cancellationToken);
            }

            // FR-013: Queue webhook notification (if configured)
            if (!string.IsNullOrEmpty(@event.WebhookUrl))
            {
                await QueueWebhookAsync(@event, cancellationToken);
            }

            _logger.LogInformation(
                "Notifications queued for payment {PaymentHash}",
                @event.PaymentHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to queue notifications for payment {PaymentHash}",
                @event.PaymentHash);
            throw;  // Rethrow to trigger event retry (if using event sourcing)
        }
    }

    /// <summary>
    /// FR-012: Queue email notification to admin with payment confirmation.
    /// </summary>
    private async Task QueueAdminEmailAsync(
        PaymentReceivedEvent @event,
        CancellationToken cancellationToken)
    {
        var adminEmail = _config["Notifications:AdminEmail"];
        if (string.IsNullOrEmpty(adminEmail))
        {
            _logger.LogWarning("No admin email configured for payment notifications");
            return;
        }

        var config = new NotificationConfiguration(
            Type: NotificationType.Email,
            Destination: adminEmail,
            MaxRetries: 5);

        try
        {
            var notificationId = await _notificationService.QueueNotificationAsync(
                @event.PaymentHash,
                config,
                cancellationToken);

            _logger.LogInformation(
                "Admin email notification queued: {NotificationId}",
                notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to queue admin email notification for payment {PaymentHash}",
                @event.PaymentHash);
            throw;
        }
    }

    /// <summary>
    /// FR-012: Queue email notification to payee/customer.
    /// </summary>
    private async Task QueuePayeeEmailAsync(
        PaymentReceivedEvent @event,
        CancellationToken cancellationToken)
    {
        var config = new NotificationConfiguration(
            Type: NotificationType.Email,
            Destination: @event.PayeeEmail!,
            MaxRetries: 5);

        try
        {
            await _notificationService.QueueNotificationAsync(
                @event.PaymentHash,
                config,
                cancellationToken);

            _logger.LogInformation(
                "Payee email notification queued for {PayeeEmail}",
                @event.PayeeEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to queue payee email notification");
            throw;
        }
    }

    /// <summary>
    /// FR-013: Queue webhook notification with HMAC signature.
    /// </summary>
    private async Task QueueWebhookAsync(
        PaymentReceivedEvent @event,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(@event.WebhookUrl))
        {
            _logger.LogWarning("No webhook URL configured");
            return;
        }

        var config = new NotificationConfiguration(
            Type: NotificationType.Webhook,
            Destination: @event.WebhookUrl,
            WebhookSecret: @event.WebhookSecret,
            MaxRetries: 5);

        try
        {
            var notificationId = await _notificationService.QueueNotificationAsync(
                @event.PaymentHash,
                config,
                cancellationToken);

            _logger.LogInformation(
                "Webhook notification queued to {WebhookUrl}: {NotificationId}",
                @event.WebhookUrl,
                notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to queue webhook notification to {WebhookUrl}",
                @event.WebhookUrl);
            throw;
        }
    }
}
```

---

## Part 2: Email Template Implementation

### Template Provider
```csharp
/// <summary>
/// Email template provider for payment notifications.
/// Renders HTML and plain-text emails with payment details.
/// </summary>
public class PaymentEmailTemplateProvider : IEmailTemplateProvider
{
    private readonly ILogger<PaymentEmailTemplateProvider> _logger;
    private readonly IConfiguration _config;

    public PaymentEmailTemplateProvider(
        IConfiguration config,
        ILogger<PaymentEmailTemplateProvider> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates admin payment confirmation email.
    /// Subject: "Payment Received: {amount} sats"
    /// </summary>
    public async Task<EmailTemplate> GetPaymentConfirmationTemplateAsync(
        PaymentNotificationModel model,
        CancellationToken cancellationToken)
    {
        var subject = $"Payment Received: {model.AmountSat:N0} sats";

        var htmlBody = await RenderPaymentConfirmationHtmlAsync(model, cancellationToken);
        var textBody = RenderPaymentConfirmationText(model);

        return new EmailTemplate(subject, htmlBody, textBody);
    }

    /// <summary>
    /// Generates refund confirmation email.
    /// </summary>
    public async Task<EmailTemplate> GetPaymentRefundTemplateAsync(
        RefundNotificationModel model,
        CancellationToken cancellationToken)
    {
        var subject = $"Refund Processed: {model.RefundAmountSat:N0} sats";

        var htmlBody = await RenderRefundHtmlAsync(model, cancellationToken);
        var textBody = RenderRefundText(model);

        return new EmailTemplate(subject, htmlBody, textBody);
    }

    /// <summary>
    /// Renders HTML email template with payment details.
    /// Uses simple string interpolation - could be replaced with Scriban, Razor, etc.
    /// </summary>
    private async Task<string> RenderPaymentConfirmationHtmlAsync(
        PaymentNotificationModel model,
        CancellationToken cancellationToken)
    {
        var siteUrl = _config["Site:Url"] ?? "https://lightning-payments.example.com";
        var supportEmail = _config["Notifications:SupportEmail"] ?? "support@example.com";

        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #f8f9fa; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ border: 1px solid #e9ecef; border-top: none; padding: 20px; }}
        .footer {{ background: #f8f9fa; border: 1px solid #e9ecef; border-top: none; padding: 20px; border-radius: 0 0 8px 8px; font-size: 12px; color: #666; }}
        .amount {{ font-size: 24px; font-weight: bold; color: #f7931a; margin: 20px 0; }}
        .detail {{ margin: 10px 0; }}
        .label {{ font-weight: 600; color: #666; }}
        .button {{ display: inline-block; background: #f7931a; color: white; padding: 10px 20px; border-radius: 4px; text-decoration: none; margin-top: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Lightning Payment Received</h1>
        </div>
        <div class='content'>
            <p>A Lightning Network payment has been received and confirmed.</p>

            <div class='amount'>{model.AmountSat:N0} sats</div>

            <div class='detail'>
                <span class='label'>Payment Hash:</span><br>
                <code>{model.PaymentHash}</code>
            </div>

            {(string.IsNullOrEmpty(model.Description) ? "" : $@"
            <div class='detail'>
                <span class='label'>Description:</span><br>
                {System.Web.HttpUtility.HtmlEncode(model.Description)}
            </div>")}

            <div class='detail'>
                <span class='label'>Received:</span><br>
                {model.ReceivedAt:g} UTC
            </div>

            <div class='detail'>
                <span class='label'>Correlation ID:</span><br>
                <code>{model.CorrelationId}</code>
            </div>

            <a href='{siteUrl}/admin/payments/{model.PaymentHash}' class='button'>
                View in Dashboard
            </a>
        </div>
        <div class='footer'>
            <p>This is an automated notification. Please don't reply to this email.</p>
            <p>For support, contact <a href='mailto:{supportEmail}'>{supportEmail}</a></p>
        </div>
    </div>
</body>
</html>";

        return await Task.FromResult(html);
    }

    /// <summary>
    /// Renders plain-text email for accessibility.
    /// </summary>
    private string RenderPaymentConfirmationText(PaymentNotificationModel model)
    {
        var text = $@"Lightning Payment Received

Amount: {model.AmountSat:N0} sats

Payment Hash: {model.PaymentHash}

{(string.IsNullOrEmpty(model.Description) ? "" : $"Description: {model.Description}\n\n")}
Received: {model.ReceivedAt:g} UTC

Correlation ID: {model.CorrelationId}

---
This is an automated notification. Please don't reply to this email.
For support, contact support@example.com";

        return text;
    }

    private async Task<string> RenderRefundHtmlAsync(
        RefundNotificationModel model,
        CancellationToken cancellationToken)
    {
        // Similar to payment confirmation, but with refund details
        // Implementation omitted for brevity
        return await Task.FromResult("<html>...</html>");
    }

    private string RenderRefundText(RefundNotificationModel model)
    {
        // Similar to payment confirmation text
        return "Refund processed...";
    }
}

/// <summary>
/// Refund notification model.
/// </summary>
public record RefundNotificationModel(
    string RecipientEmail,
    string OriginalPaymentHash,
    long OriginalAmountSat,
    long RefundAmountSat,
    string? Reason,
    DateTime ProcessedAt);
```

---

## Part 3: Webhook Payload Structure

### Webhook Event Models
```csharp
/// <summary>
/// Base webhook event with standardized metadata.
/// Follows pattern similar to Stripe, GitHub, etc.
/// </summary>
public record WebhookEvent<TPayload>
{
    /// <summary>
    /// Unique identifier for this webhook delivery attempt.
    /// Allows receiver to deduplicate retries.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Event type identifier.
    /// Example: "payment.received", "payment.failed", "refund.processed"
    /// </summary>
    public string Type { get; init; } = null!;

    /// <summary>
    /// Unix timestamp when event was created.
    /// Used for replay protection on receiver side.
    /// </summary>
    public long Timestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>
    /// API version for this payload structure.
    /// Allows backward compatibility if schema changes.
    /// </summary>
    public string ApiVersion { get; init; } = "1.0";

    /// <summary>
    /// Event-specific data.
    /// </summary>
    public TPayload Data { get; init; } = null!;
}

/// <summary>
/// Payment received webhook event payload.
/// Sent when a Lightning invoice is paid.
/// </summary>
public record PaymentReceivedPayload
{
    /// <summary>
    /// Payment hash from the Lightning invoice.
    /// Uniquely identifies this payment within the network.
    /// </summary>
    public string PaymentHash { get; init; } = null!;

    /// <summary>
    /// Amount received in satoshis.
    /// Note: Never include millisatoshi values in webhooks - always use satoshis for simplicity.
    /// </summary>
    public long AmountSat { get; init; }

    /// <summary>
    /// Optional fee amount in satoshis (if applicable).
    /// </summary>
    public long? FeeSat { get; init; }

    /// <summary>
    /// Invoice description from the original invoice.
    /// May contain order ID, content description, etc.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Timestamp when payment was received.
    /// ISO 8601 format: "2026-01-26T12:34:56Z"
    /// </summary>
    public string ReceivedAt { get; init; } = null!;

    /// <summary>
    /// Correlation ID from original payment request.
    /// Links this webhook to application session/transaction.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Custom metadata from invoice (if provided).
    /// Free-form JSON object for integration-specific data.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Example webhook payloads.
/// </summary>
public static class WebhookPayloadExamples
{
    /// <summary>
    /// Minimal payment received webhook (required fields only).
    /// </summary>
    public static readonly WebhookEvent<PaymentReceivedPayload> MinimalPaymentReceived = new()
    {
        Id = "evt_1A2B3C",
        Type = "payment.received",
        Timestamp = 1706256896,
        ApiVersion = "1.0",
        Data = new PaymentReceivedPayload
        {
            PaymentHash = "abc123def456",
            AmountSat = 100000,
            ReceivedAt = "2026-01-26T12:34:56Z"
        }
    };

    /// <summary>
    /// Complete payment received webhook with all fields.
    /// </summary>
    public static readonly WebhookEvent<PaymentReceivedPayload> CompletePaymentReceived = new()
    {
        Id = "evt_2D4E5F",
        Type = "payment.received",
        Timestamp = 1706256896,
        ApiVersion = "1.0",
        Data = new PaymentReceivedPayload
        {
            PaymentHash = "abc123def456",
            AmountSat = 100000,
            FeeSat = 2000,
            Description = "Order #12345 - Content Access",
            ReceivedAt = "2026-01-26T12:34:56Z",
            CorrelationId = "sess_abc123",
            Metadata = new Dictionary<string, object>
            {
                { "orderId", "12345" },
                { "contentId", "page_789" },
                { "userId", "user_456" },
                { "sessionId", "sess_abc123" }
            }
        }
    };
}
```

### Webhook Delivery with Payload
```csharp
/// <summary>
/// Extension of webhook delivery service to include full event payload.
/// </summary>
public static class WebhookDeliveryExtensions
{
    /// <summary>
    /// Delivers a payment received webhook event.
    /// Includes HMAC signature and timestamp for validation.
    /// </summary>
    public static async Task<WebhookDeliveryResult> DeliverPaymentReceivedWebhookAsync(
        this IWebhookDeliveryService service,
        string webhookUrl,
        PaymentReceivedEvent payment,
        string? webhookSecret,
        CancellationToken cancellationToken = default)
    {
        var payload = new WebhookEvent<PaymentReceivedPayload>
        {
            Id = $"evt_{Guid.NewGuid():N}".Substring(0, 20),
            Type = "payment.received",
            ApiVersion = "1.0",
            Data = new PaymentReceivedPayload
            {
                PaymentHash = payment.PaymentHash,
                AmountSat = payment.AmountSat,
                Description = payment.Description,
                ReceivedAt = payment.ReceivedAt.ToString("O"),  // ISO 8601
                CorrelationId = payment.CorrelationId
            }
        };

        return await service.DeliverAsync(
            webhookUrl,
            new PaymentEventPayload(
                payload.Type,
                payload.Data.PaymentHash,
                payload.Data.AmountSat,
                payment.ReceivedAt,
                payment.Description,
                JsonSerializer.Serialize(payload.Data)),
            webhookSecret,
            cancellationToken);
    }
}
```

---

## Part 4: Admin UI for Notification Management

### Notification Dashboard DTO
```csharp
/// <summary>
/// Data transfer object for notification dashboard display.
/// Used by backoffice UI to show notification status and history.
/// </summary>
public record NotificationDashboardDto
{
    public Guid NotificationId { get; init; }
    public string PaymentHash { get; init; } = null!;
    public string Type { get; init; } = null!;  // "Email" or "Webhook"
    public string Destination { get; init; } = null!;
    public string Status { get; init; } = null!;  // "Pending", "Delivered", "Failed"
    public int AttemptCount { get; init; }
    public int MaxRetries { get; init; }
    public DateTime? LastAttemptAt { get; init; }
    public string? LastError { get; init; }
    public DateTime? NextRetryAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }

    /// <summary>
    /// Human-readable status with icon/color for UI.
    /// </summary>
    public string StatusDisplay => Status switch
    {
        "Delivered" => "✓ Delivered",
        "Pending" => "⏳ Pending",
        "Failed" => "✗ Failed",
        _ => Status
    };

    /// <summary>
    /// Time until next retry, or null if not pending retry.
    /// </summary>
    public TimeSpan? TimeUntilRetry =>
        NextRetryAt.HasValue ? NextRetryAt.Value - DateTime.UtcNow : null;

    public bool CanManuallyRetry => Status == "Failed";
}

/// <summary>
/// Query service for dashboard UI.
/// </summary>
public interface INotificationDashboardService
{
    /// <summary>
    /// Gets notifications for a specific payment.
    /// </summary>
    Task<IEnumerable<NotificationDashboardDto>> GetPaymentNotificationsAsync(
        string paymentHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed notifications across all payments.
    /// Used for admin dashboard to show issues requiring attention.
    /// </summary>
    Task<IEnumerable<NotificationDashboardDto>> GetFailedNotificationsAsync(
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending notifications due for retry soon.
    /// </summary>
    Task<IEnumerable<NotificationDashboardDto>> GetPendingRetryNotificationsAsync(
        TimeSpan window = default,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of dashboard service.
/// </summary>
public class NotificationDashboardService : INotificationDashboardService
{
    private readonly IPaymentNotificationRepository _repository;

    public NotificationDashboardService(IPaymentNotificationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IEnumerable<NotificationDashboardDto>> GetPaymentNotificationsAsync(
        string paymentHash,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _repository.GetByPaymentHashAsync(paymentHash, cancellationToken);

        return notifications.Select(MapToDto).OrderByDescending(x => x.CreatedAt);
    }

    public async Task<IEnumerable<NotificationDashboardDto>> GetFailedNotificationsAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        // Would need to add method to repository for this query
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<NotificationDashboardDto>> GetPendingRetryNotificationsAsync(
        TimeSpan window = default,
        CancellationToken cancellationToken = default)
    {
        var pending = await _repository.GetPendingForRetryAsync(cancellationToken);

        if (window == default)
            window = TimeSpan.FromMinutes(5);

        var soon = pending
            .Where(x => x.NextRetryAt.HasValue &&
                   x.NextRetryAt.Value <= DateTime.UtcNow.Add(window))
            .Select(MapToDto)
            .OrderBy(x => x.NextRetryAt);

        return await Task.FromResult(soon);
    }

    private NotificationDashboardDto MapToDto(PaymentNotification notification)
    {
        return new NotificationDashboardDto
        {
            NotificationId = notification.Id,
            PaymentHash = notification.PaymentHash,
            Type = notification.Type.ToString(),
            Destination = notification.Destination,
            Status = notification.Status.ToString(),
            AttemptCount = notification.AttemptCount,
            MaxRetries = notification.MaxRetries,
            LastAttemptAt = notification.LastAttemptAt,
            LastError = notification.LastErrorMessage,
            NextRetryAt = notification.NextRetryAt,
            CreatedAt = notification.CreatedAt,
            DeliveredAt = notification.DeliveredAt
        };
    }
}
```

### Admin Controller (API Endpoint)
```csharp
/// <summary>
/// API endpoints for notification management in backoffice.
/// Secured with [Authorize] attribute and admin policy.
/// </summary>
[ApiController]
[Route("api/v1/admin/notifications")]
[Authorize(Policy = "AdminOnly")]
public class NotificationManagementController : ControllerBase
{
    private readonly IPaymentNotificationService _notificationService;
    private readonly INotificationDashboardService _dashboardService;
    private readonly ILogger<NotificationManagementController> _logger;

    public NotificationManagementController(
        IPaymentNotificationService notificationService,
        INotificationDashboardService dashboardService,
        ILogger<NotificationManagementController> logger)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get notifications for a specific payment.
    /// GET /api/v1/admin/notifications/payment/{paymentHash}
    /// </summary>
    [HttpGet("payment/{paymentHash}")]
    public async Task<ActionResult<IEnumerable<NotificationDashboardDto>>> GetPaymentNotifications(
        string paymentHash,
        CancellationToken cancellationToken)
    {
        try
        {
            var notifications = await _dashboardService.GetPaymentNotificationsAsync(
                paymentHash,
                cancellationToken);

            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for payment {PaymentHash}", paymentHash);
            return StatusCode(500, "Failed to retrieve notifications");
        }
    }

    /// <summary>
    /// Get all failed notifications requiring attention.
    /// GET /api/v1/admin/notifications/failed?limit=50
    /// </summary>
    [HttpGet("failed")]
    public async Task<ActionResult<IEnumerable<NotificationDashboardDto>>> GetFailedNotifications(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notifications = await _dashboardService.GetFailedNotificationsAsync(limit, cancellationToken);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving failed notifications");
            return StatusCode(500, "Failed to retrieve notifications");
        }
    }

    /// <summary>
    /// Get pending notifications scheduled for retry soon.
    /// GET /api/v1/admin/notifications/pending-retry?windowMinutes=5
    /// </summary>
    [HttpGet("pending-retry")]
    public async Task<ActionResult<IEnumerable<NotificationDashboardDto>>> GetPendingRetryNotifications(
        [FromQuery] int windowMinutes = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var window = TimeSpan.FromMinutes(windowMinutes);
            var notifications = await _dashboardService.GetPendingRetryNotificationsAsync(
                window,
                cancellationToken);

            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending retry notifications");
            return StatusCode(500, "Failed to retrieve notifications");
        }
    }

    /// <summary>
    /// Manually retry a failed notification.
    /// POST /api/v1/admin/notifications/{notificationId}/retry
    /// </summary>
    [HttpPost("{notificationId:guid}/retry")]
    public async Task<ActionResult> RetryNotification(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Admin requesting manual retry for notification {NotificationId}",
                notificationId);

            var success = await _notificationService.RetryFailedNotificationAsync(
                notificationId,
                cancellationToken);

            if (success)
            {
                return Accepted();  // 202 Accepted - retry initiated
            }
            else
            {
                return NotFound();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying notification {NotificationId}", notificationId);
            return StatusCode(500, "Failed to retry notification");
        }
    }
}
```

---

## Part 5: Monitoring & Alerts

### Health Check Implementation
```csharp
/// <summary>
/// Health check for notification system.
/// Monitors email and webhook delivery capabilities.
/// </summary>
public class NotificationServiceHealthCheck : IHealthCheck
{
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<NotificationServiceHealthCheck> _logger;

    public NotificationServiceHealthCheck(
        IEmailNotificationService emailService,
        ILogger<NotificationServiceHealthCheck> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test SMTP connectivity by sending a test email
            var result = await _emailService.SendPaymentConfirmationAsync(
                new PaymentNotificationModel(
                    "health-check@internal",
                    "health_check_test",
                    1,
                    "Health check test",
                    DateTime.UtcNow
                ),
                cancellationToken);

            if (result.Success)
            {
                return HealthCheckResult.Healthy("Notification service is healthy");
            }
            else
            {
                return HealthCheckResult.Degraded(
                    "Notification service degraded",
                    new Exception(result.ErrorMessage));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification health check failed");
            return HealthCheckResult.Unhealthy(
                "Notification service is unhealthy",
                ex);
        }
    }
}
```

### Metrics & Observability
```csharp
/// <summary>
/// OpenTelemetry metrics for notification system.
/// </summary>
public static class NotificationMetrics
{
    private static readonly ActivitySource ActivitySource =
        new("PaymentNotifications", "1.0.0");

    private static readonly Counter<long> NotificationsQueued =
        null!;  // Initialize in DI

    private static readonly Counter<long> NotificationsDelivered =
        null!;

    private static readonly Counter<long> NotificationsFailed =
        null!;

    private static readonly Histogram<double> DeliveryLatency =
        null!;

    /// <summary>
    /// Record notification queued event.
    /// </summary>
    public static void RecordNotificationQueued(string type, string destination)
    {
        using (var activity = ActivitySource.StartActivity("notification.queued"))
        {
            activity?.SetTag("notification.type", type);
            activity?.SetTag("notification.destination", destination);
        }
    }

    /// <summary>
    /// Record notification delivered event.
    /// </summary>
    public static void RecordNotificationDelivered(
        string type,
        int attemptCount,
        long durationMs)
    {
        using (var activity = ActivitySource.StartActivity("notification.delivered"))
        {
            activity?.SetTag("notification.type", type);
            activity?.SetTag("notification.attempts", attemptCount);
            activity?.SetTag("notification.latency_ms", durationMs);
        }
    }

    /// <summary>
    /// Record notification failed event.
    /// </summary>
    public static void RecordNotificationFailed(
        string type,
        int attemptCount,
        string reason)
    {
        using (var activity = ActivitySource.StartActivity("notification.failed"))
        {
            activity?.SetTag("notification.type", type);
            activity?.SetTag("notification.attempts", attemptCount);
            activity?.SetTag("notification.failure_reason", reason);
        }
    }
}
```

---

## Part 6: Example Usage in Payment Service

```csharp
/// <summary>
/// Complete example of notification integration in payment service.
/// </summary>
public class LightningPaymentService
{
    private readonly IBreezSdkService _breezSdk;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentNotificationService _notificationService;
    private readonly IPublisher _eventPublisher;
    private readonly ILogger<LightningPaymentService> _logger;

    public LightningPaymentService(
        IBreezSdkService breezSdk,
        IPaymentRepository paymentRepository,
        IPaymentNotificationService notificationService,
        IPublisher eventPublisher,
        ILogger<LightningPaymentService> logger)
    {
        _breezSdk = breezSdk ?? throw new ArgumentNullException(nameof(breezSdk));
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles payment received from BreezSDK listener.
    /// Implements end-to-end flow: receive → record → notify.
    /// </summary>
    public async Task ProcessPaymentReceivedAsync(
        PaymentInfo paymentInfo,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing payment: Hash={PaymentHash}, Amount={Amount} sats",
            paymentInfo.Hash,
            paymentInfo.AmountMsat / 1000);

        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // 1. Record payment in database
            var payment = new Payment
            {
                PaymentHash = paymentInfo.Hash,
                AmountSat = paymentInfo.AmountMsat / 1000,
                FeeSat = paymentInfo.FeeMsat / 1000,
                ReceivedAt = DateTime.UtcNow,
                CorrelationId = correlationId,
                Status = PaymentStatus.Confirmed
            };

            await _paymentRepository.AddAsync(payment, cancellationToken);
            await _paymentRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment recorded: {PaymentId}. CorrelationId: {CorrelationId}",
                payment.Id,
                correlationId);

            // 2. Publish domain event to trigger notifications
            var @event = new PaymentReceivedEvent
            {
                PaymentHash = paymentInfo.Hash,
                AmountSat = paymentInfo.AmountMsat / 1000,
                Description = paymentInfo.Description,
                ReceivedAt = DateTime.UtcNow,
                PayeeEmail = payment.PayeeEmail,
                WebhookUrl = payment.WebhookUrl,
                WebhookSecret = payment.WebhookSecret,
                CorrelationId = correlationId
            };

            await _eventPublisher.Publish(@event, cancellationToken);

            _logger.LogInformation(
                "Payment received event published. CorrelationId: {CorrelationId}",
                correlationId);

            // 3. Event handler (PaymentReceivedEventHandler) will queue notifications
            // This is decoupled from payment processing for resilience
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process payment {PaymentHash}",
                paymentInfo.Hash);
            throw;
        }
    }
}
```

---

## Summary

This document demonstrates:

1. **Integration with BreezSDK** - Payment received events trigger notifications
2. **Email Notification** - HTML and text templates for admins and payees
3. **Webhook Notifications** - Standardized event format with HMAC signing
4. **Admin Dashboard** - UI for viewing and manually retrying failed notifications
5. **Monitoring** - Health checks and metrics for operational visibility

All patterns follow the Constitution principles and production best practices for .NET 9.0.

**References**:
- FR-012: Email notifications (implemented via IEmailNotificationService)
- FR-013: Webhook notifications (implemented via IWebhookDeliveryService)
- FR-014: Retry policy (Polly with exponential backoff)
- FR-015: Logging and manual retry (PaymentNotification entity + admin API)
