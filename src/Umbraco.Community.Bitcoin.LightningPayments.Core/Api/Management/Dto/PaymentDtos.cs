using System.Text.Json.Serialization;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management.Dto;

/// <summary>
/// Paginated response for payment list.
/// </summary>
public record PaymentListResponse
{
    /// <summary>
    /// List of payment summaries.
    /// </summary>
    public IReadOnlyList<PaymentSummaryDto> Items { get; init; } = Array.Empty<PaymentSummaryDto>();

    /// <summary>
    /// Total number of payments matching the filter.
    /// </summary>
    public int Total { get; init; }
}

/// <summary>
/// Summary information for a payment.
/// </summary>
public record PaymentSummaryDto
{
    /// <summary>
    /// The unique payment hash.
    /// </summary>
    public string PaymentHash { get; init; } = string.Empty;

    /// <summary>
    /// Amount in satoshis.
    /// </summary>
    public long AmountSat { get; init; }

    /// <summary>
    /// Current payment status.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentStatus Status { get; init; }

    /// <summary>
    /// Type of payment (paywall or tip).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentKind Kind { get; init; }

    /// <summary>
    /// Content ID the payment is associated with (null for tips).
    /// </summary>
    public int? ContentId { get; init; }

    /// <summary>
    /// Name of the content (populated from Umbraco if available).
    /// </summary>
    public string? ContentName { get; init; }

    /// <summary>
    /// When the payment was created/initiated.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Detailed payment information including related data.
/// </summary>
public record PaymentDetailsDto : PaymentSummaryDto
{
    /// <summary>
    /// The user's session ID.
    /// </summary>
    public string UserSessionId { get; init; } = string.Empty;

    /// <summary>
    /// ID of the Bolt12 offer this payment was received against (if any).
    /// </summary>
    public Guid? Bolt12OfferId { get; init; }

    /// <summary>
    /// Summary of refunds issued against this payment.
    /// </summary>
    public IReadOnlyList<RefundSummaryDto> Refunds { get; init; } = Array.Empty<RefundSummaryDto>();

    /// <summary>
    /// Summary of notifications sent for this payment.
    /// </summary>
    public IReadOnlyList<NotificationSummaryDto> Notifications { get; init; } = Array.Empty<NotificationSummaryDto>();
}

/// <summary>
/// Summary of a refund transaction.
/// </summary>
public record RefundSummaryDto
{
    /// <summary>
    /// Unique refund identifier.
    /// </summary>
    public Guid RefundId { get; init; }

    /// <summary>
    /// Amount refunded in satoshis.
    /// </summary>
    public long AmountSat { get; init; }

    /// <summary>
    /// Current refund status.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// When the refund was initiated.
    /// </summary>
    public DateTimeOffset InitiatedAt { get; init; }
}

/// <summary>
/// Summary of a notification.
/// </summary>
public record NotificationSummaryDto
{
    /// <summary>
    /// Unique notification identifier.
    /// </summary>
    public Guid NotificationId { get; init; }

    /// <summary>
    /// The payment hash this notification is for.
    /// </summary>
    public string PaymentHash { get; init; } = string.Empty;

    /// <summary>
    /// Type of notification (email or webhook).
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// The event that triggered this notification.
    /// </summary>
    public string Event { get; init; } = string.Empty;

    /// <summary>
    /// Current delivery status.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Number of delivery attempts made.
    /// </summary>
    public int AttemptCount { get; init; }

    /// <summary>
    /// When the notification was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the notification was successfully sent (null if not sent).
    /// </summary>
    public DateTimeOffset? SentAt { get; init; }

    /// <summary>
    /// Last error message if delivery failed.
    /// </summary>
    public string? LastError { get; init; }
}
