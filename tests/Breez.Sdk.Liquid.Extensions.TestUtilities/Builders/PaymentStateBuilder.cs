using Breez.Sdk.Liquid.Extensions.Core.Domain;

namespace Breez.Sdk.Liquid.Extensions.TestUtilities.Builders;

/// <summary>
/// Fluent builder for creating PaymentState instances for testing.
/// </summary>
public class PaymentStateBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _paymentHash = GeneratePaymentHash();
    private PaymentStatus _status = PaymentStatus.Pending;
    private ulong _amountSat = 1000;
    private string? _description;
    private string? _invoice;
    private PaymentKind _kind = PaymentKind.Custom;
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private DateTimeOffset? _confirmedAt;
    private DateTimeOffset? _expiresAt;
    private string? _preimage;
    private ulong? _feeSat;
    private readonly Dictionary<string, string> _metadata = new();
    private string? _correlationId;

    /// <summary>
    /// Creates a new builder with default values.
    /// </summary>
    /// <returns>A new PaymentStateBuilder instance.</returns>
    public static PaymentStateBuilder Create() => new();

    /// <summary>
    /// Creates a new builder for a pending payment.
    /// </summary>
    /// <param name="amountSat">The amount in satoshis. Defaults to 1000.</param>
    /// <returns>A PaymentStateBuilder configured for a pending payment.</returns>
    public static PaymentStateBuilder Pending(ulong amountSat = 1000) =>
        new PaymentStateBuilder()
            .WithStatus(PaymentStatus.Pending)
            .WithAmount(amountSat)
            .WithExpiresAt(DateTimeOffset.UtcNow.AddHours(1));

    /// <summary>
    /// Creates a new builder for a succeeded payment.
    /// </summary>
    /// <param name="amountSat">The amount in satoshis. Defaults to 1000.</param>
    /// <returns>A PaymentStateBuilder configured for a succeeded payment.</returns>
    public static PaymentStateBuilder Succeeded(ulong amountSat = 1000) =>
        new PaymentStateBuilder()
            .WithStatus(PaymentStatus.Succeeded)
            .WithAmount(amountSat)
            .WithConfirmedAt(DateTimeOffset.UtcNow)
            .WithPreimage(GeneratePreimage());

    /// <summary>
    /// Creates a new builder for a failed payment.
    /// </summary>
    /// <param name="amountSat">The amount in satoshis. Defaults to 1000.</param>
    /// <returns>A PaymentStateBuilder configured for a failed payment.</returns>
    public static PaymentStateBuilder Failed(ulong amountSat = 1000) =>
        new PaymentStateBuilder()
            .WithStatus(PaymentStatus.Failed)
            .WithAmount(amountSat);

    /// <summary>
    /// Sets the payment ID.
    /// </summary>
    /// <param name="id">The unique identifier for the payment.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PaymentStateBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the payment hash.
    /// </summary>
    /// <param name="paymentHash">The payment hash (64-character hex string).</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PaymentStateBuilder WithPaymentHash(string paymentHash)
    {
        _paymentHash = paymentHash;
        return this;
    }

    /// <summary>
    /// Sets the payment status.
    /// </summary>
    /// <param name="status">The current status of the payment.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PaymentStateBuilder WithStatus(PaymentStatus status)
    {
        _status = status;
        return this;
    }

    /// <summary>
    /// Sets the payment amount in satoshis.
    /// </summary>
    /// <param name="amountSat">The payment amount in satoshis.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PaymentStateBuilder WithAmount(ulong amountSat)
    {
        _amountSat = amountSat;
        return this;
    }

    /// <summary>
    /// Sets the payment description.
    /// </summary>
    /// <param name="description">Optional description or memo for the payment.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PaymentStateBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the invoice string.
    /// </summary>
    /// <param name="invoice">The Lightning invoice string (BOLT11).</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PaymentStateBuilder WithInvoice(string? invoice)
    {
        _invoice = invoice;
        return this;
    }

    /// <summary>
    /// Sets the payment kind.
    /// </summary>
    /// <param name="kind">The type of payment (Custom, Paywall, TipJar, Purchase, Subscription).</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PaymentStateBuilder WithKind(PaymentKind kind)
    {
        _kind = kind;
        return this;
    }

    /// <summary>
    /// Sets the creation timestamp.
    /// </summary>
    /// <param name="createdAt">The timestamp when the payment was created.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PaymentStateBuilder WithCreatedAt(DateTimeOffset createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    /// <summary>
    /// Sets the confirmation timestamp.
    /// </summary>
    /// <param name="confirmedAt">The timestamp when the payment was confirmed, or null if not confirmed.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PaymentStateBuilder WithConfirmedAt(DateTimeOffset? confirmedAt)
    {
        _confirmedAt = confirmedAt;
        return this;
    }

    /// <summary>
    /// Sets the expiration timestamp.
    /// </summary>
    /// <param name="expiresAt">The timestamp when the payment expires, or null if no expiration.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PaymentStateBuilder WithExpiresAt(DateTimeOffset? expiresAt)
    {
        _expiresAt = expiresAt;
        return this;
    }

    /// <summary>
    /// Sets the preimage.
    /// </summary>
    /// <param name="preimage">The payment preimage (64-character hex string), or null if not available.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PaymentStateBuilder WithPreimage(string? preimage)
    {
        _preimage = preimage;
        return this;
    }

    /// <summary>
    /// Sets the fee in satoshis.
    /// </summary>
    /// <param name="feeSat">The fee paid for the payment in satoshis, or null if not applicable.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PaymentStateBuilder WithFee(ulong? feeSat)
    {
        _feeSat = feeSat;
        return this;
    }

    /// <summary>
    /// Adds metadata to the payment.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PaymentStateBuilder WithMetadata(string key, string value)
    {
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the correlation ID.
    /// </summary>
    /// <param name="correlationId">An optional correlation ID for tracking related operations.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public PaymentStateBuilder WithCorrelationId(string? correlationId)
    {
        _correlationId = correlationId;
        return this;
    }

    /// <summary>
    /// Builds the PaymentState instance with the configured values.
    /// </summary>
    /// <returns>A new PaymentState instance.</returns>
    public PaymentState Build()
    {
        return new PaymentState
        {
            Id = _id,
            PaymentHash = _paymentHash,
            Status = _status,
            AmountSat = _amountSat,
            Description = _description,
            Invoice = _invoice,
            Kind = _kind,
            CreatedAt = _createdAt,
            ConfirmedAt = _confirmedAt,
            ExpiresAt = _expiresAt,
            Preimage = _preimage,
            FeeSat = _feeSat,
            Metadata = new Dictionary<string, string>(_metadata),
            CorrelationId = _correlationId
        };
    }

    /// <summary>
    /// Generates a random payment hash for testing purposes.
    /// </summary>
    /// <returns>A 64-character hex string representing a payment hash.</returns>
    private static string GeneratePaymentHash()
    {
        return (Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"))[..64];
    }

    /// <summary>
    /// Generates a random preimage for testing purposes.
    /// </summary>
    /// <returns>A 64-character hex string representing a preimage.</returns>
    private static string GeneratePreimage()
    {
        return (Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"))[..64];
    }
}
