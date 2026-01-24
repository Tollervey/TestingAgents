using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Infrastructure;
using Breez.Sdk.Liquid.Extensions.Core.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Breez.Sdk.Liquid.Extensions.Core.Extensions;

/// <summary>
/// Extension methods for registering BreezSDK services in the dependency injection container.
/// </summary>
/// <remarks>
/// Provides fluent API methods to configure BreezSDK services with either inline configuration
/// or from IConfiguration sources. Supports both online and offline modes.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds BreezSDK services to the service collection with inline configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="BreezSdkOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is null.
    /// </exception>
    /// <remarks>
    /// This method registers the following services as singletons:
    /// <list type="bullet">
    ///   <item><see cref="IBreezSdkWrapper"/> → <see cref="BreezSdkWrapper"/></item>
    ///   <item><see cref="IBreezSdkService"/> → <see cref="BreezSdkService"/></item>
    ///   <item><see cref="IPaymentRepository"/> → <see cref="InMemoryPaymentRepository"/> (if not already registered)</item>
    /// </list>
    /// Options validation is automatically configured using <see cref="BreezSdkOptionsValidator"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddBreezSdk(options =>
    /// {
    ///     options.ApiKey = "your-api-key";
    ///     options.Mnemonic = "your twelve word mnemonic phrase here";
    ///     options.Network = BreezNetwork.Mainnet;
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddBreezSdk(
        this IServiceCollection services,
        Action<BreezSdkOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // Configure options with validation
        services.Configure(configureOptions);
        services.AddSingleton<IValidateOptions<BreezSdkOptions>, BreezSdkOptionsValidator>();

        // Register logging if not already registered
        services.AddLogging();

        // Register core services
        services.AddSingleton<IBreezSdkWrapper, BreezSdkWrapper>();
        services.AddSingleton<IBreezSdkService, BreezSdkService>();

        // Register default repository (can be overridden by consumer)
        services.TryAddSingleton<IPaymentRepository, InMemoryPaymentRepository>();

        return services;
    }

    /// <summary>
    /// Adds BreezSDK services to the service collection with configuration from IConfiguration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configurationSection">The configuration section containing BreezSDK settings.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configurationSection"/> is null.
    /// </exception>
    /// <remarks>
    /// This method binds configuration from the provided section to <see cref="BreezSdkOptions"/>.
    /// The configuration section should contain properties matching the option names (e.g., ApiKey, Mnemonic).
    /// </remarks>
    /// <example>
    /// <code>
    /// // In appsettings.json:
    /// // {
    /// //   "BreezSdk": {
    /// //     "ApiKey": "your-api-key",
    /// //     "Mnemonic": "your twelve word mnemonic phrase here",
    /// //     "Network": "Mainnet"
    /// //   }
    /// // }
    ///
    /// services.AddBreezSdk(configuration.GetSection("BreezSdk"));
    /// </code>
    /// </example>
    public static IServiceCollection AddBreezSdk(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        // Bind configuration section to options
        services.Configure<BreezSdkOptions>(configurationSection);
        services.AddSingleton<IValidateOptions<BreezSdkOptions>, BreezSdkOptionsValidator>();

        // Register logging if not already registered
        services.AddLogging();

        // Register core services
        services.AddSingleton<IBreezSdkWrapper, BreezSdkWrapper>();
        services.AddSingleton<IBreezSdkService, BreezSdkService>();

        // Register default repository (can be overridden by consumer)
        services.TryAddSingleton<IPaymentRepository, InMemoryPaymentRepository>();

        return services;
    }

    /// <summary>
    /// Adds BreezSDK services in offline mode with inline configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="BreezSdkOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is null.
    /// </exception>
    /// <remarks>
    /// This method registers the following services as singletons:
    /// <list type="bullet">
    ///   <item><see cref="IBreezSdkService"/> → <see cref="OfflineBreezSdkService"/></item>
    ///   <item><see cref="IPaymentRepository"/> → <see cref="InMemoryPaymentRepository"/> (if not already registered)</item>
    /// </list>
    /// Unlike <see cref="AddBreezSdk(IServiceCollection, Action{BreezSdkOptions})"/>, this method does NOT register
    /// <see cref="IBreezSdkWrapper"/> since offline mode does not interact with the native SDK.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddBreezSdkOffline(options =>
    /// {
    ///     options.ApiKey = "test-api-key";
    ///     options.Mnemonic = "test mnemonic phrase with twelve words here";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddBreezSdkOffline(
        this IServiceCollection services,
        Action<BreezSdkOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // Configure options (offline mode can still use validation)
        services.Configure(configureOptions);
        services.AddSingleton<IValidateOptions<BreezSdkOptions>, BreezSdkOptionsValidator>();

        // Register logging if not already registered
        services.AddLogging();

        // Register offline service (no wrapper needed)
        services.AddSingleton<IBreezSdkService, OfflineBreezSdkService>();

        // Register default repository (can be overridden by consumer)
        services.TryAddSingleton<IPaymentRepository, InMemoryPaymentRepository>();

        return services;
    }

    /// <summary>
    /// Adds BreezSDK services in offline mode with configuration from IConfiguration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configurationSection">The configuration section containing BreezSDK settings.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configurationSection"/> is null.
    /// </exception>
    /// <remarks>
    /// This method binds configuration from the provided section to <see cref="BreezSdkOptions"/>.
    /// Offline mode uses <see cref="OfflineBreezSdkService"/> which does not interact with the native SDK.
    /// </remarks>
    /// <example>
    /// <code>
    /// // In appsettings.json:
    /// // {
    /// //   "BreezSdk": {
    /// //     "ApiKey": "test-api-key",
    /// //     "Mnemonic": "test mnemonic phrase with twelve words here"
    /// //   }
    /// // }
    ///
    /// services.AddBreezSdkOffline(configuration.GetSection("BreezSdk"));
    /// </code>
    /// </example>
    public static IServiceCollection AddBreezSdkOffline(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        // Bind configuration section to options
        services.Configure<BreezSdkOptions>(configurationSection);
        services.AddSingleton<IValidateOptions<BreezSdkOptions>, BreezSdkOptionsValidator>();

        // Register logging if not already registered
        services.AddLogging();

        // Register offline service (no wrapper needed)
        services.AddSingleton<IBreezSdkService, OfflineBreezSdkService>();

        // Register default repository (can be overridden by consumer)
        services.TryAddSingleton<IPaymentRepository, InMemoryPaymentRepository>();

        return services;
    }
}
