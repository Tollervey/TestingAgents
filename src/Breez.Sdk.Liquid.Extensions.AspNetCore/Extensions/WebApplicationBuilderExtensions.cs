using Breez.Sdk.Liquid.Extensions.AspNetCore.HealthChecks;
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Extensions;
using Breez.Sdk.Liquid.Extensions.Core.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Breez.Sdk.Liquid.Extensions.AspNetCore.Extensions;

/// <summary>
/// Extension methods for configuring BreezSDK with WebApplicationBuilder.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds BreezSDK services to the WebApplicationBuilder.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder.</param>
    /// <param name="configure">Optional configuration action for BreezSdkOptions.</param>
    /// <returns>The WebApplicationBuilder for chaining.</returns>
    /// <remarks>
    /// This method:
    /// - Registers core BreezSDK services
    /// - Registers the ASP.NET Core health check implementation
    /// - Binds configuration from the "BreezSdk" section
    /// </remarks>
    public static WebApplicationBuilder AddBreezSdk(
        this WebApplicationBuilder builder,
        Action<BreezSdkOptions>? configure = null)
    {
        // Add core BreezSDK services
        builder.Services.AddBreezSdk(options =>
        {
            // Bind from configuration
            builder.Configuration.GetSection(BreezSdkOptions.SectionName).Bind(options);

            // Apply custom configuration
            configure?.Invoke(options);
        });

        // Register ASP.NET Core specific health check
        builder.Services.AddSingleton<BreezSdkHealthCheck>();

        // Register payment event channel for webhook processing
        builder.Services.TryAddSingleton<IPaymentEventChannel, PaymentEventChannel>();

        return builder;
    }

    /// <summary>
    /// Adds BreezSDK services in offline mode for development and testing.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder.</param>
    /// <param name="configure">Optional configuration action for BreezSdkOptions.</param>
    /// <returns>The WebApplicationBuilder for chaining.</returns>
    public static WebApplicationBuilder AddBreezSdkOffline(
        this WebApplicationBuilder builder,
        Action<BreezSdkOptions>? configure = null)
    {
        // Add core BreezSDK services in offline mode
        builder.Services.AddBreezSdkOffline(options =>
        {
            // Bind from configuration
            builder.Configuration.GetSection(BreezSdkOptions.SectionName).Bind(options);

            // Apply custom configuration
            configure?.Invoke(options);
        });

        // Register ASP.NET Core specific health check
        builder.Services.AddSingleton<BreezSdkHealthCheck>();

        // Register payment event channel for webhook processing
        builder.Services.TryAddSingleton<IPaymentEventChannel, PaymentEventChannel>();

        return builder;
    }

    /// <summary>
    /// Adds BreezSDK services using a configuration section.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder.</param>
    /// <param name="configurationSection">The configuration section containing BreezSDK options.</param>
    /// <returns>The WebApplicationBuilder for chaining.</returns>
    public static WebApplicationBuilder AddBreezSdk(
        this WebApplicationBuilder builder,
        IConfigurationSection configurationSection)
    {
        builder.Services.AddBreezSdk(configurationSection);
        builder.Services.AddSingleton<BreezSdkHealthCheck>();
        builder.Services.TryAddSingleton<IPaymentEventChannel, PaymentEventChannel>();
        return builder;
    }
}
