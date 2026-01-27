using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

/// <summary>
/// Represents a reusable BOLT12 offer for accepting multiple payments.
/// </summary>
public class Bolt12Offer
{
    /// <summary>
    /// Unique identifier for the offer.
    /// </summary>
    [Key]
    public Guid OfferId { get; set; }

    /// <summary>
    /// The BOLT12 offer string (starts with "lno1").
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string OfferString { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the offer.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Fixed amount in satoshis (null for variable amount offers).
    /// </summary>
    public ulong? AmountSat { get; set; }

    /// <summary>
    /// Whether the offer is currently accepting payments.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the offer was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the offer was deactivated (null if still active).
    /// </summary>
    public DateTimeOffset? DeactivatedAt { get; set; }

    /// <summary>
    /// Optional: Content ID this offer is associated with.
    /// </summary>
    public int? ContentId { get; set; }

    /// <summary>
    /// Payments received against this offer.
    /// </summary>
    public virtual ICollection<PaymentState> Payments { get; set; } = new List<PaymentState>();
}
