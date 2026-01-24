namespace Breez.Sdk.Liquid.Extensions.Core.Domain;

/// <summary>
/// Payment status enumeration.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment is awaiting confirmation.
    /// </summary>
    Pending,

    /// <summary>
    /// Payment has been confirmed.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Payment failed permanently.
    /// </summary>
    Failed,

    /// <summary>
    /// Payment invoice has expired.
    /// </summary>
    Expired,

    /// <summary>
    /// Payment was refunded.
    /// </summary>
    Refunded
}
