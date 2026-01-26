using System.ComponentModel.DataAnnotations;

namespace Breez.Sdk.Liquid.Extensions.Core.Configuration;

/// <summary>
/// Circuit breaker configuration options for protecting against cascading failures.
/// Implements the circuit breaker pattern to prevent repeated calls to failing operations.
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Gets or sets the number of consecutive failures before the circuit opens.
    /// When this threshold is reached within the sampling duration, the circuit breaks
    /// to prevent further calls to the failing operation.
    /// Default: 5
    /// </summary>
    /// <remarks>
    /// The circuit breaker counts failures within the <see cref="SamplingDurationSeconds"/> window.
    /// Once this threshold is reached, the circuit opens and rejects subsequent calls for
    /// <see cref="BreakDurationSeconds"/> seconds.
    /// </remarks>
    [Range(1, 100)]
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the window duration in seconds for counting failures.
    /// Failures are counted only within this rolling time window.
    /// Default: 60
    /// </summary>
    /// <remarks>
    /// This creates a sliding window for failure detection. If <see cref="FailureThreshold"/>
    /// failures occur within this duration, the circuit opens. Older failures outside
    /// this window are not counted.
    /// </remarks>
    [Range(10, 600)]
    public int SamplingDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the duration in seconds to keep the circuit open after it breaks.
    /// During this period, all calls are immediately rejected without attempting the operation.
    /// Default: 30
    /// </summary>
    /// <remarks>
    /// After the circuit opens due to failures, it remains open (rejecting all calls) for
    /// this duration. Once this time elapses, the circuit transitions to half-open state,
    /// allowing a test call to determine if the underlying issue has been resolved.
    /// </remarks>
    [Range(5, 300)]
    public int BreakDurationSeconds { get; set; } = 30;
}
