using System.ComponentModel.DataAnnotations.Schema;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models
{
    /// <summary>
    /// Represents the status of a payment.
    /// </summary>
    public enum PaymentStatus { Pending, Paid, Failed, Expired, RefundPending, Refunded }

    /// <summary>
    /// Classifies the kind of payment.
    /// </summary>
    public enum PaymentKind { Paywall, Tip }

    /// <summary>
    /// Represents the state of a payment for content access.
    /// </summary>
    public class PaymentState
    {
        /// <summary>
        /// The unique hash of the payment.
        /// </summary>
        public string PaymentHash { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the content item. Optional for tips;0 when not associated.
        /// </summary>
        public int ContentId { get; set; }

        /// <summary>
        /// The user's session ID.
        /// </summary>
        public string UserSessionId { get; set; } = string.Empty;

        /// <summary>
        /// The current status of the payment.
        /// </summary>
        public PaymentStatus Status { get; set; }

        /// <summary>
        /// The amount of the payment in satoshis.
        /// </summary>
        public ulong AmountSat { get; set; }

        /// <summary>
        /// The kind of the payment (Paywall or Tip).
        /// </summary>
        public PaymentKind Kind { get; set; }

        /// <summary>
        /// Optional: Bolt12 offer this payment was received against.
        /// </summary>
        public Guid? Bolt12OfferId { get; set; }

        /// <summary>
        /// Navigation to the associated offer.
        /// </summary>
        [ForeignKey(nameof(Bolt12OfferId))]
        public virtual Bolt12Offer? Bolt12Offer { get; set; }

        /// <summary>
        /// Refunds issued against this payment.
        /// </summary>
        public virtual ICollection<RefundTransaction> Refunds { get; set; } = new List<RefundTransaction>();

        /// <summary>
        /// Notifications sent for this payment.
        /// </summary>
        public virtual ICollection<PaymentNotification> Notifications { get; set; } = new List<PaymentNotification>();
    }
}

