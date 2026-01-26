using System.ComponentModel.DataAnnotations;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;

/// <summary>
/// Configuration options for payment notifications (email and webhook).
/// </summary>
public class NotificationOptions
{
    /// <summary>
    /// The configuration section name in appsettings.
    /// </summary>
    public const string SectionName = "LightningPayments:Notifications";

    /// <summary>
    /// Whether notifications are enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Email notification configuration.
    /// </summary>
    public EmailNotificationOptions Email { get; set; } = new();

    /// <summary>
    /// Webhook notification configuration.
    /// </summary>
    public WebhookNotificationOptions Webhook { get; set; } = new();

    /// <summary>
    /// Retry policy configuration.
    /// </summary>
    public NotificationRetryOptions Retry { get; set; } = new();
}

/// <summary>
/// Configuration options for email notifications.
/// </summary>
public class EmailNotificationOptions
{
    /// <summary>
    /// Whether email notifications are enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The email address to send notifications to.
    /// </summary>
    [EmailAddress]
    public string? RecipientEmail { get; set; }

    /// <summary>
    /// The sender email address.
    /// </summary>
    [EmailAddress]
    public string? SenderEmail { get; set; }

    /// <summary>
    /// The sender display name.
    /// </summary>
    public string SenderName { get; set; } = "Lightning Payments";

    /// <summary>
    /// SMTP host for sending emails.
    /// </summary>
    public string? SmtpHost { get; set; }

    /// <summary>
    /// SMTP port.
    /// </summary>
    [Range(1, 65535)]
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Whether to use SSL/TLS for SMTP.
    /// </summary>
    public bool SmtpUseSsl { get; set; } = true;

    /// <summary>
    /// SMTP username for authentication.
    /// </summary>
    public string? SmtpUsername { get; set; }

    /// <summary>
    /// SMTP password for authentication.
    /// </summary>
    public string? SmtpPassword { get; set; }

    /// <summary>
    /// Events to send email notifications for.
    /// </summary>
    public NotificationEventFilter Events { get; set; } = new();
}

/// <summary>
/// Configuration options for webhook notifications.
/// </summary>
public class WebhookNotificationOptions
{
    /// <summary>
    /// Whether webhook notifications are enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The webhook URL to send notifications to.
    /// </summary>
    [Url]
    public string? Url { get; set; }

    /// <summary>
    /// The secret key for HMAC-SHA256 signature verification.
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Timeout in seconds for webhook HTTP requests.
    /// </summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Events to send webhook notifications for.
    /// </summary>
    public NotificationEventFilter Events { get; set; } = new();
}

/// <summary>
/// Filter for which notification events to send.
/// </summary>
public class NotificationEventFilter
{
    /// <summary>
    /// Send notification when payment is confirmed.
    /// </summary>
    public bool PaymentConfirmed { get; set; } = true;

    /// <summary>
    /// Send notification when payment fails.
    /// </summary>
    public bool PaymentFailed { get; set; } = true;

    /// <summary>
    /// Send notification when payment expires.
    /// </summary>
    public bool PaymentExpired { get; set; } = false;

    /// <summary>
    /// Send notification when refund is initiated.
    /// </summary>
    public bool RefundInitiated { get; set; } = true;

    /// <summary>
    /// Send notification when refund is completed.
    /// </summary>
    public bool RefundCompleted { get; set; } = true;
}

/// <summary>
/// Configuration options for notification retry policy.
/// </summary>
public class NotificationRetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts.
    /// </summary>
    [Range(1, 10)]
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Initial delay in seconds before first retry.
    /// </summary>
    [Range(1, 300)]
    public int InitialDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Maximum delay in seconds between retries.
    /// </summary>
    [Range(1, 3600)]
    public int MaxDelaySeconds { get; set; } = 3600;

    /// <summary>
    /// Multiplier for exponential backoff.
    /// </summary>
    [Range(1.0, 5.0)]
    public double BackoffMultiplier { get; set; } = 2.0;
}
