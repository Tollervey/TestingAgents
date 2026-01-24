namespace Breez.Sdk.Liquid.Extensions.Core.Domain.Events;

/// <summary>
/// Raised when an invoice is created.
/// </summary>
/// <remarks>
/// This event is published when a new Lightning invoice (BOLT11 or BOLT12) is successfully generated.
/// It contains the invoice string and metadata required for payment tracking.
/// </remarks>
public sealed record InvoiceCreated : PaymentEvent
{
    /// <summary>
    /// Gets the BOLT11 or BOLT12 invoice string.
    /// </summary>
    /// <value>
    /// The encoded invoice string that can be used to make a payment.
    /// This is typically a long alphanumeric string beginning with 'lnbc' (BOLT11) or other prefixes (BOLT12).
    /// </value>
    public required string Invoice { get; init; }

    /// <summary>
    /// Gets the requested amount in satoshis.
    /// </summary>
    /// <value>
    /// The invoice amount denominated in satoshis (1 BTC = 100,000,000 satoshis).
    /// Must be greater than zero.
    /// </value>
    public required ulong AmountSat { get; init; }

    /// <summary>
    /// Gets the human-readable description for the invoice.
    /// </summary>
    /// <value>
    /// An optional description that provides context about the payment.
    /// May be null if no description was provided.
    /// Maximum length is typically 500 characters.
    /// </value>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the invoice expires.
    /// </summary>
    /// <value>
    /// A <see cref="DateTimeOffset"/> representing when the invoice becomes invalid and can no longer be paid.
    /// Lightning invoices typically expire within 24 hours of creation.
    /// </value>
    public DateTimeOffset ExpiresAt { get; init; }
}
