using System.ComponentModel.DataAnnotations;

namespace Breez.Sdk.Liquid.Extensions.Core.Configuration;

/// <summary>
/// Configuration options for automatic reconnection behavior.
/// </summary>
public class ReconnectionOptions
{
    /// <summary>
    /// Maximum number of reconnection attempts before entering Failed state.
    /// </summary>
    [Range(1, 100)]
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Initial delay in milliseconds before first reconnection attempt.
    /// </summary>
    [Range(100, 60000)]
    public int InitialDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum delay in milliseconds between reconnection attempts.
    /// </summary>
    [Range(1000, 300000)]
    public int MaxDelayMs { get; set; } = 30000;

    /// <summary>
    /// Multiplier for exponential backoff between reconnection attempts.
    /// </summary>
    [Range(1.0, 5.0)]
    public double BackoffMultiplier { get; set; } = 2.0;
}
