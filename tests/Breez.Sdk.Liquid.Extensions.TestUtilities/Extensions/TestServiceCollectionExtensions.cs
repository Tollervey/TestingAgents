using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.TestUtilities.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace Breez.Sdk.Liquid.Extensions.TestUtilities.Extensions;

/// <summary>
/// Extension methods for configuring BreezSDK services in test scenarios.
/// </summary>
/// <remarks>
/// These extensions simplify the setup of test dependencies by providing
/// convenient methods to register fake implementations, mock services,
/// and replace existing service registrations in the DI container.
/// </remarks>
public static class TestServiceCollectionExtensions
{
    /// <summary>
    /// Adds a fake BreezSDK wrapper for testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method registers <see cref="FakeBreezSdkWrapper"/> as both its concrete type
    /// and the <see cref="IBreezSdkWrapper"/> interface. This allows tests to access
    /// the fake's additional test methods while still supporting standard DI resolution.
    /// <para>
    /// The fake wrapper provides in-memory simulation of BreezSDK operations without
    /// requiring network connectivity or a real Lightning wallet.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.AddFakeBreezSdk();
    /// var provider = services.BuildServiceProvider();
    /// var wrapper = provider.GetRequiredService&lt;IBreezSdkWrapper&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddFakeBreezSdk(this IServiceCollection services)
    {
        services.AddSingleton<FakeBreezSdkWrapper>();
        services.AddSingleton<IBreezSdkWrapper>(sp => sp.GetRequiredService<FakeBreezSdkWrapper>());
        return services;
    }

    /// <summary>
    /// Adds a fake BreezSDK wrapper with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for the fake wrapper.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This overload allows customization of the fake wrapper before registration,
    /// enabling tests to set up specific scenarios such as error conditions,
    /// custom balances, or simulated delays.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddFakeBreezSdk(fake =>
    /// {
    ///     fake.Balance = 50_000; // Set custom balance
    ///     fake.SimulatedDelay = TimeSpan.FromMilliseconds(100); // Add latency
    ///     fake.PrepareReceivePaymentException = new Exception("Test error"); // Simulate failure
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddFakeBreezSdk(
        this IServiceCollection services,
        Action<FakeBreezSdkWrapper> configure)
    {
        var fake = new FakeBreezSdkWrapper();
        configure(fake);
        services.AddSingleton(fake);
        services.AddSingleton<IBreezSdkWrapper>(fake);
        return services;
    }

    /// <summary>
    /// Adds a mock BreezSDK service for testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="mockService">The mock service instance.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method registers a mock implementation of <see cref="IBreezSdkService"/>.
    /// Use this when you need fine-grained control over service behavior with
    /// mocking frameworks like Moq or NSubstitute.
    /// <para>
    /// Unlike <see cref="AddFakeBreezSdk(IServiceCollection)"/>, which registers the SDK wrapper layer,
    /// this method operates at the service layer for higher-level testing scenarios.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var mockService = new Mock&lt;IBreezSdkService&gt;();
    /// mockService.Setup(s => s.IsConnected).Returns(true);
    ///
    /// services.AddMockBreezSdkService(mockService.Object);
    /// </code>
    /// </example>
    public static IServiceCollection AddMockBreezSdkService(
        this IServiceCollection services,
        IBreezSdkService mockService)
    {
        services.AddSingleton(mockService);
        return services;
    }

    /// <summary>
    /// Replaces an existing service registration with a new implementation.
    /// </summary>
    /// <typeparam name="TService">The service type to replace.</typeparam>
    /// <typeparam name="TImplementation">The new implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method removes any existing registration for <typeparamref name="TService"/>
    /// and adds a new singleton registration with <typeparamref name="TImplementation"/>.
    /// <para>
    /// Use this to swap out production services with test doubles in integration tests
    /// where the service collection is pre-configured.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Replace production service with test implementation
    /// services.ReplaceService&lt;IBreezSdkWrapper, FakeBreezSdkWrapper&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection ReplaceService<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TService));
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.AddSingleton<TService, TImplementation>();
        return services;
    }

    /// <summary>
    /// Replaces an existing service registration with a specific instance.
    /// </summary>
    /// <typeparam name="TService">The service type to replace.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="instance">The instance to use.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method removes any existing registration for <typeparamref name="TService"/>
    /// and adds a new singleton registration with the provided instance.
    /// <para>
    /// This is useful when you have a pre-configured mock or fake that needs to
    /// replace an existing service registration, particularly in scenarios where
    /// you're testing against a partially-configured service collection.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var fake = new FakeBreezSdkWrapper();
    /// fake.Balance = 100_000;
    ///
    /// // Replace with pre-configured instance
    /// services.ReplaceService&lt;IBreezSdkWrapper&gt;(fake);
    /// </code>
    /// </example>
    public static IServiceCollection ReplaceService<TService>(
        this IServiceCollection services,
        TService instance)
        where TService : class
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TService));
        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.AddSingleton(instance);
        return services;
    }
}
