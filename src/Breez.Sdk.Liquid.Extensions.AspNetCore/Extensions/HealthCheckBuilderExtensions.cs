using Breez.Sdk.Liquid.Extensions.AspNetCore.HealthChecks;
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Breez.Sdk.Liquid.Extensions.AspNetCore.Extensions;

/// <summary>
/// Extension methods for adding BreezSDK health checks.
/// </summary>
public static class HealthCheckBuilderExtensions
{
    /// <summary>
    /// The default name for the BreezSDK health check.
    /// </summary>
    public const string DefaultHealthCheckName = "breez-sdk";

    /// <summary>
    /// Adds a health check for BreezSDK connectivity.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">The health check name. Defaults to "breez-sdk".</param>
    /// <param name="failureStatus">The failure status. Defaults to Unhealthy.</param>
    /// <param name="tags">Optional tags to categorize the health check.</param>
    /// <param name="timeout">Optional timeout for the health check.</param>
    /// <returns>The health checks builder for chaining.</returns>
    /// <remarks>
    /// This health check verifies that the BreezSDK is connected and operational.
    /// It requires that <see cref="IBreezHealthCheck"/> is registered in the service collection.
    /// </remarks>
    public static IHealthChecksBuilder AddBreezSdkHealthCheck(
        this IHealthChecksBuilder builder,
        string? name = null,
        HealthStatus failureStatus = HealthStatus.Unhealthy,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name ?? DefaultHealthCheckName,
            sp =>
            {
                var healthCheck = sp.GetRequiredService<IBreezHealthCheck>();
                return new BreezSdkHealthCheck(healthCheck);
            },
            failureStatus,
            tags,
            timeout));
    }

    /// <summary>
    /// Adds a health check for BreezSDK connectivity with custom tags.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="tags">Tags to categorize the health check (e.g., "ready", "live").</param>
    /// <returns>The health checks builder for chaining.</returns>
    public static IHealthChecksBuilder AddBreezSdkHealthCheck(
        this IHealthChecksBuilder builder,
        params string[] tags)
    {
        return builder.AddBreezSdkHealthCheck(
            name: DefaultHealthCheckName,
            failureStatus: HealthStatus.Unhealthy,
            tags: tags);
    }
}
