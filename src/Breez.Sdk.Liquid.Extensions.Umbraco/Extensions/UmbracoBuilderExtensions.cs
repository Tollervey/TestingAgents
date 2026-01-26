using Breez.Sdk.Liquid.Extensions.AspNetCore.Extensions;
using Breez.Sdk.Liquid.Extensions.AspNetCore.HealthChecks;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;

namespace Breez.Sdk.Liquid.Extensions.Umbraco;

/// <summary>
/// Extension methods for configuring BreezSDK with Umbraco.
/// </summary>
public static class UmbracoBuilderExtensions
{
    /// <summary>
    /// Adds BreezSDK services to the Umbraco application.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <param name="configure">Optional configuration action for BreezSdkOptions.</param>
    /// <returns>The Umbraco builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers:
    /// - Core BreezSDK services (IBreezSdkService, etc.)
    /// - ASP.NET Core health check implementation
    /// - Configuration binding from the "BreezSdk" section
    /// </para>
    /// <para>
    /// If BreezSdk:OfflineMode is true in configuration, the offline mock service
    /// will be registered instead of the live service.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In a custom composer:
    /// public void Compose(IUmbracoBuilder builder)
    /// {
    ///     builder.AddBreezSdk(options =>
    ///     {
    ///         options.Network = BreezNetwork.Testnet;
    ///     });
    /// }
    /// </code>
    /// </example>
    public static IUmbracoBuilder AddBreezSdk(
        this IUmbracoBuilder builder,
        Action<BreezSdkOptions>? configure = null)
    {
        // Check if offline mode is configured
        var offlineMode = builder.Config.GetValue<bool>("BreezSdk:OfflineMode");

        if (offlineMode)
        {
            // Register offline services
            builder.Services.AddBreezSdkOffline(options =>
            {
                builder.Config.GetSection(BreezSdkOptions.SectionName).Bind(options);
                configure?.Invoke(options);
            });
        }
        else
        {
            // Register live services
            builder.Services.AddBreezSdk(options =>
            {
                builder.Config.GetSection(BreezSdkOptions.SectionName).Bind(options);
                configure?.Invoke(options);
            });
        }

        // Register ASP.NET Core specific health check
        builder.Services.AddSingleton<BreezSdkHealthCheck>();

        // Register Umbraco health check
        builder.Services.AddHealthChecks()
            .AddBreezSdkHealthCheck("breez-sdk", tags: new[] { "ready" });

        return builder;
    }

    /// <summary>
    /// Adds BreezSDK services in offline mode for development and testing.
    /// </summary>
    /// <param name="builder">The Umbraco builder.</param>
    /// <param name="configure">Optional configuration action for BreezSdkOptions.</param>
    /// <returns>The Umbraco builder for chaining.</returns>
    public static IUmbracoBuilder AddBreezSdkOffline(
        this IUmbracoBuilder builder,
        Action<BreezSdkOptions>? configure = null)
    {
        builder.Services.AddBreezSdkOffline(options =>
        {
            builder.Config.GetSection(BreezSdkOptions.SectionName).Bind(options);
            configure?.Invoke(options);
        });

        builder.Services.AddSingleton<BreezSdkHealthCheck>();

        return builder;
    }
}
