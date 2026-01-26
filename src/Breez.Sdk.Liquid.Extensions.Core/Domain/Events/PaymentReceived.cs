namespace Breez.Sdk.Liquid.Extensions.Core.Domain.Events;

/// <summary>
/// Raised when a payment is successfully received.
/// </summary>
/// <remarks>
/// This event indicates that a Lightning payment has been successfully received
/// and settled. The preimage may be present if the payment protocol requires it.
/// </remarks>
public sealed record PaymentReceived : PaymentEvent
{
    /// <summary>
    /// Gets the amount received in satoshis.
    /// </summary>
    /// <value>
    /// The payment amount in satoshis (1 BTC = 100,000,000 satoshis).
    /// This value must be greater than zero.
    /// </value>
    public required ulong AmountSat { get; init; }

    /// <summary>
    /// Gets the preimage revealed by the payer.
    /// </summary>
    /// <value>
    /// A hex-encoded string representing the payment preimage, or null if not available.
    /// The preimage is the secret value that proves payment was received.
    /// </value>
    /// <remarks>
    /// <para>
    /// <b>Security Warning:</b> This is a cryptographic secret. Never serialize this event
    /// to logs, persistent storage, or external systems without proper redaction.
    /// </para>
    /// <para>
    /// The preimage MUST only be used in-memory for immediate payment verification.
    /// If you need to persist proof of payment, store only the payment hash.
    /// </para>
    /// </remarks>
    public string? Preimage { get; init; }
}
