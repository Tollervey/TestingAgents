namespace Breez.Sdk.Liquid.Extensions.Core.Domain;

/// <summary>
/// Represents the persistent state of a payment transaction.
/// </summary>
public class PaymentState
{
    /// <summary>
    /// Unique identifier for the payment record.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The payment hash (hex-encoded) that uniquely identifies this payment in the Lightning network.
    /// </summary>
    public required string PaymentHash { get; init; }

    /// <summary>
    /// Current status of the payment.
    /// </summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>
    /// Payment amount in satoshis.
    /// </summary>
    public required ulong AmountSat { get; init; }

    /// <summary>
    /// Human-readable description of the payment.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The BOLT11 invoice string (if applicable).
    /// </summary>
    public string? Invoice { get; init; }

    /// <summary>
    /// Type of payment (e.g., Paywall, TipJar, Custom).
    /// </summary>
    public PaymentKind Kind { get; init; } = PaymentKind.Custom;

    /// <summary>
    /// UTC timestamp when the payment was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// UTC timestamp when the payment was confirmed (if applicable).
    /// </summary>
    public DateTimeOffset? ConfirmedAt { get; set; }

    /// <summary>
    /// UTC timestamp when the payment expires (for invoices).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Preimage revealed upon successful payment (hex-encoded).
    /// </summary>
    public string? Preimage { get; set; }

    /// <summary>
    /// Fee paid in satoshis (for outgoing payments).
    /// </summary>
    public ulong? FeeSat { get; set; }

    /// <summary>
    /// Custom metadata dictionary for application-specific data.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }
}
