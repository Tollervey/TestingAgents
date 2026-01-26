using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

/// <summary>
/// Represents a notification (email or webhook) for a payment event.
/// </summary>
public class PaymentNotification
{
    /// <summary>
    /// Unique identifier for the notification.
    /// </summary>
    [Key]
    public Guid NotificationId { get; set; }

    /// <summary>
    /// Payment hash this notification relates to.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string PaymentHash { get; set; } = string.Empty;

    /// <summary>
    /// Type of notification (Email or Webhook).
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Event that triggered the notification.
    /// </summary>
    public NotificationEvent Event { get; set; }

    /// <summary>
    /// Destination (email address or webhook URL).
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Destination { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the notification.
    /// </summary>
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    /// <summary>
    /// Number of delivery attempts made.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Maximum number of attempts before marking as permanently failed.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Last error message if delivery failed.
    /// </summary>
    [MaxLength(1000)]
    public string? LastError { get; set; }

    /// <summary>
    /// When the notification was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the next retry should be attempted.
    /// </summary>
    public DateTimeOffset? NextRetryAt { get; set; }

    /// <summary>
    /// When the notification was successfully sent.
    /// </summary>
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>
    /// When the notification permanently failed.
    /// </summary>
    public DateTimeOffset? FailedAt { get; set; }

    /// <summary>
    /// JSON payload sent (for debugging/retry).
    /// </summary>
    [MaxLength(4000)]
    public string? Payload { get; set; }

    /// <summary>
    /// HTTP response status code (for webhooks).
    /// </summary>
    public int? ResponseStatusCode { get; set; }

    /// <summary>
    /// Navigation to the related payment.
    /// </summary>
    [ForeignKey(nameof(PaymentHash))]
    public virtual PaymentState? Payment { get; set; }
}

/// <summary>
/// Type of notification delivery.
/// </summary>
public enum NotificationType
{
    Email = 0,
    Webhook = 1
}

/// <summary>
/// Event that triggered the notification.
/// </summary>
public enum NotificationEvent
{
    PaymentConfirmed = 0,
    PaymentFailed = 1,
    PaymentExpired = 2,
    RefundInitiated = 3,
    RefundCompleted = 4
}

/// <summary>
/// Status of notification delivery.
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Notification created, waiting to be sent.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Notification successfully delivered.
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Delivery failed, will retry.
    /// </summary>
    Retrying = 2,

    /// <summary>
    /// Permanently failed after max attempts.
    /// </summary>
    Failed = 3
}
