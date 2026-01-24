namespace Breez.Sdk.Liquid.Extensions.Core.Domain.Events;

/// <summary>
/// Raised when a payment fails.
/// </summary>
public sealed record PaymentFailed : PaymentEvent
{
    /// <summary>
    /// Error code identifying the failure type.
    /// </summary>
    public required BreezErrorCode ErrorCode { get; init; }

    /// <summary>
    /// Human-readable failure reason.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Whether the payment operation can be retried.
    /// </summary>
    public bool IsRetryable { get; init; }
}
