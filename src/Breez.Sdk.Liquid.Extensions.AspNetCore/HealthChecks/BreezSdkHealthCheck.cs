using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Breez.Sdk.Liquid.Extensions.AspNetCore.HealthChecks;

/// <summary>
/// ASP.NET Core health check implementation for BreezSDK.
/// </summary>
/// <remarks>
/// This health check implements <see cref="IHealthCheck"/> and delegates to
/// <see cref="IBreezHealthCheck"/> for the actual health checking logic.
/// It maps the BreezHealthStatus to the appropriate HealthCheckResult.
/// </remarks>
public class BreezSdkHealthCheck : IHealthCheck
{
    private readonly IBreezHealthCheck _breezHealthCheck;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreezSdkHealthCheck"/> class.
    /// </summary>
    /// <param name="breezHealthCheck">The BreezSDK health check service.</param>
    public BreezSdkHealthCheck(IBreezHealthCheck breezHealthCheck)
    {
        _breezHealthCheck = breezHealthCheck ?? throw new ArgumentNullException(nameof(breezHealthCheck));
    }

    /// <summary>
    /// Runs the health check asynchronously.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="HealthCheckResult"/> representing the health status.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _breezHealthCheck.CheckHealthAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["duration_ms"] = status.Duration.TotalMilliseconds
            };

            // Include any additional data from the health status
            if (status.Data != null)
            {
                foreach (var kvp in status.Data)
                {
                    data[kvp.Key] = kvp.Value;
                }
            }

            if (!status.IsHealthy)
            {
                return HealthCheckResult.Unhealthy(
                    status.Message ?? "SDK not connected",
                    data: data);
            }

            // Check for degraded state (healthy but with issues)
            if (status.IsConnected && status.Message != null &&
                !status.Message.Equals("SDK connected", StringComparison.OrdinalIgnoreCase))
            {
                // If there's a custom message but the status is healthy, it might be degraded
                // We consider it degraded if the message indicates a non-optimal state
                if (status.Message.Contains("latency", StringComparison.OrdinalIgnoreCase) ||
                    status.Message.Contains("degraded", StringComparison.OrdinalIgnoreCase) ||
                    status.Message.Contains("slow", StringComparison.OrdinalIgnoreCase))
                {
                    return HealthCheckResult.Degraded(
                        status.Message,
                        data: data);
                }
            }

            return HealthCheckResult.Healthy(
                status.Message ?? "SDK connected",
                data: data);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "SDK health check failed",
                exception: ex);
        }
    }
}
