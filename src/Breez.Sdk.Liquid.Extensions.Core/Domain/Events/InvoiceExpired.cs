namespace Breez.Sdk.Liquid.Extensions.Core.Domain.Events;

/// <summary>
/// Raised when an invoice expires without being paid.
/// </summary>
/// <remarks>
/// This event is published when a Lightning invoice reaches its expiration time without receiving payment.
/// Once expired, the invoice can no longer be paid and the payment state should be marked as expired.
/// </remarks>
public sealed record InvoiceExpired : PaymentEvent
{
    /// <summary>
    /// Gets the UTC timestamp when the invoice expired.
    /// </summary>
    /// <value>
    /// A <see cref="DateTimeOffset"/> representing the exact moment when the invoice became invalid.
    /// This timestamp is used to track when invoices transition to the expired state.
    /// </value>
    public DateTimeOffset ExpiredAt { get; init; }
}
