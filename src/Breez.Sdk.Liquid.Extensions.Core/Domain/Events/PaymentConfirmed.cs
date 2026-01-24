namespace Breez.Sdk.Liquid.Extensions.Core.Domain.Events;

/// <summary>
/// Raised when a payment is confirmed.
/// </summary>
/// <remarks>
/// This event indicates that a payment has been successfully confirmed on the network.
/// It includes the confirmed amount, preimage proof, and any fees paid for outgoing payments.
/// </remarks>
public sealed record PaymentConfirmed : PaymentEvent
{
    /// <summary>
    /// Gets the confirmed amount in satoshis.
    /// </summary>
    /// <value>
    /// The actual amount that was confirmed for the payment.
    /// </value>
    public required ulong AmountSat { get; init; }

    /// <summary>
    /// Gets the preimage proving payment completion.
    /// </summary>
    /// <value>
    /// A hex-encoded string representing the payment preimage.
    /// This cryptographic proof confirms successful payment settlement.
    /// </value>
    public required string Preimage { get; init; }

    /// <summary>
    /// Gets the fee paid in satoshis for outgoing payments.
    /// </summary>
    /// <value>
    /// The network fee paid for the payment transaction.
    /// This is null for incoming payments (received payments).
    /// </value>
    public ulong? FeeSat { get; init; }
}
