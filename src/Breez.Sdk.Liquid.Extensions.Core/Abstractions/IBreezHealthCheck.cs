namespace Breez.Sdk.Liquid.Extensions.Core.Abstractions;

/// <summary>
/// Interface for BreezSDK health checking.
/// </summary>
public interface IBreezHealthCheck
{
    /// <summary>
    /// Checks whether the SDK is connected and operational.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health check result.</returns>
    Task<BreezHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the health status of the BreezSDK.
/// </summary>
public record BreezHealthStatus
{
    /// <summary>
    /// Whether the SDK is healthy and operational.
    /// </summary>
    public bool IsHealthy { get; init; }

    /// <summary>
    /// Whether the SDK is connected to the network.
    /// </summary>
    public bool IsConnected { get; init; }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Additional diagnostic data.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Data { get; init; }

    /// <summary>
    /// Time taken to perform the health check.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Creates a healthy status.
    /// </summary>
    /// <param name="message">Optional custom message. Defaults to "SDK connected".</param>
    /// <param name="duration">Time taken to perform the health check.</param>
    /// <returns>A healthy status instance.</returns>
    public static BreezHealthStatus Healthy(string? message = null, TimeSpan duration = default)
        => new() { IsHealthy = true, IsConnected = true, Message = message ?? "SDK connected", Duration = duration };

    /// <summary>
    /// Creates a degraded status (connected but with issues).
    /// </summary>
    /// <param name="message">Description of the degraded state.</param>
    /// <param name="duration">Time taken to perform the health check.</param>
    /// <returns>A degraded status instance.</returns>
    public static BreezHealthStatus Degraded(string message, TimeSpan duration = default)
        => new() { IsHealthy = true, IsConnected = true, Message = message, Duration = duration };

    /// <summary>
    /// Creates an unhealthy status.
    /// </summary>
    /// <param name="message">Description of why the SDK is unhealthy.</param>
    /// <param name="duration">Time taken to perform the health check.</param>
    /// <returns>An unhealthy status instance.</returns>
    public static BreezHealthStatus Unhealthy(string message, TimeSpan duration = default)
        => new() { IsHealthy = false, IsConnected = false, Message = message, Duration = duration };
}
