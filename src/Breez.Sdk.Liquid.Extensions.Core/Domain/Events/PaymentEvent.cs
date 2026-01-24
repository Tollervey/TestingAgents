namespace Breez.Sdk.Liquid.Extensions.Core.Domain.Events;

/// <summary>
/// Base class for all payment-related domain events.
/// </summary>
/// <remarks>
/// Payment events represent significant state changes in the payment lifecycle.
/// All derived events inherit common properties for tracking and correlation.
/// </remarks>
public abstract record PaymentEvent
{
    /// <summary>
    /// Gets the payment hash that uniquely identifies the payment.
    /// </summary>
    /// <value>
    /// A hex-encoded string representing the payment hash.
    /// This value is immutable once set.
    /// </value>
    public required string PaymentHash { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    /// <value>
    /// A <see cref="DateTimeOffset"/> in UTC timezone.
    /// Defaults to the current UTC time when the event is created.
    /// </value>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the optional correlation ID for distributed tracing and event correlation.
    /// </summary>
    /// <value>
    /// A unique identifier that can be used to correlate events across service boundaries.
    /// May be null if distributed tracing is not enabled.
    /// </value>
    public string? CorrelationId { get; init; }
}
