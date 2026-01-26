namespace Breez.Sdk.Liquid.Extensions.Core.Domain;

/// <summary>
/// Represents a Lightning invoice or offer.
/// </summary>
/// <remarks>
/// An invoice is a payment request that can be either a BOLT11 standard invoice
/// or a BOLT12 reusable offer. It contains all necessary information for making
/// a Lightning payment including the destination, amount, and expiration time.
/// </remarks>
public record Invoice
{
    /// <summary>
    /// Gets the payment hash (hex-encoded) that uniquely identifies this invoice.
    /// </summary>
    /// <remarks>
    /// The payment hash is used to track the payment throughout its lifecycle
    /// and serves as a unique identifier for the invoice.
    /// </remarks>
    public required string PaymentHash { get; init; }

    /// <summary>
    /// Gets the encoded invoice or offer string (BOLT11 or BOLT12).
    /// </summary>
    /// <remarks>
    /// This is the actual invoice string that should be shared with the payer.
    /// For BOLT11 invoices, this starts with "lnbc" (mainnet) or "lntb" (testnet).
    /// For BOLT12 offers, this is the encoded offer string.
    /// </remarks>
    public required string Destination { get; init; }

    /// <summary>
    /// Gets the type of invoice.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="InvoiceType.Bolt11"/> for standard Lightning invoices.
    /// </remarks>
    public InvoiceType Type { get; init; } = InvoiceType.Bolt11;

    /// <summary>
    /// Gets the requested amount in satoshis.
    /// </summary>
    /// <remarks>
    /// This is the amount that will be paid if the invoice is successfully settled.
    /// Does not include network fees.
    /// </remarks>
    public required ulong AmountSat { get; init; }

    /// <summary>
    /// Gets the human-readable description of the payment.
    /// </summary>
    /// <remarks>
    /// Optional field that provides context about what the payment is for.
    /// This is typically displayed to users when showing invoice details.
    /// </remarks>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the invoice was created.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="DateTimeOffset.UtcNow"/> when the invoice is instantiated.
    /// </remarks>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the UTC timestamp when the invoice expires.
    /// </summary>
    /// <remarks>
    /// After this time, the invoice can no longer be paid. Most Lightning wallets
    /// and nodes will reject payment attempts for expired invoices.
    /// </remarks>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Gets the estimated fees that will be charged on successful payment (in satoshis).
    /// </summary>
    /// <remarks>
    /// Optional field that may be populated during invoice creation to inform
    /// the user of expected routing fees. The actual fee may differ when the
    /// payment is executed.
    /// </remarks>
    public ulong? FeeSat { get; init; }
}
