using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

/// <summary>
/// Represents a refund transaction linked to an original payment.
/// </summary>
public class RefundTransaction
{
    /// <summary>
    /// Unique identifier for the refund.
    /// </summary>
    [Key]
    public Guid RefundId { get; set; }

    /// <summary>
    /// Payment hash of the original payment being refunded.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string OriginalPaymentHash { get; set; } = string.Empty;

    /// <summary>
    /// Amount being refunded in satoshis.
    /// </summary>
    public ulong AmountSat { get; set; }

    /// <summary>
    /// BOLT11 invoice provided by the customer for the refund.
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string DestinationInvoice { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the refund.
    /// </summary>
    public RefundStatus Status { get; set; } = RefundStatus.Pending;

    /// <summary>
    /// Error message if the refund failed.
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Reason for the refund (provided by admin).
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary>
    /// Umbraco user ID who initiated the refund.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string InitiatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// When the refund was initiated.
    /// </summary>
    public DateTimeOffset InitiatedAt { get; set; }

    /// <summary>
    /// When the refund completed (success or failure).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Payment hash of the outbound refund payment (from SDK).
    /// </summary>
    [MaxLength(64)]
    public string? RefundPaymentHash { get; set; }

    /// <summary>
    /// Navigation to the original payment.
    /// </summary>
    [ForeignKey(nameof(OriginalPaymentHash))]
    public virtual PaymentState? OriginalPayment { get; set; }
}

/// <summary>
/// Status of a refund transaction.
/// </summary>
public enum RefundStatus
{
    /// <summary>
    /// Refund has been initiated but not yet processed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Refund payment was sent successfully.
    /// </summary>
    Succeeded = 1,

    /// <summary>
    /// Refund failed (see ErrorMessage for details).
    /// </summary>
    Failed = 2
}
