using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Configuration;

/// <summary>
/// Tests for fail-fast configuration validation behavior.
/// These tests verify that invalid configurations are detected at application startup
/// rather than deferred until first use.
/// </summary>
/// <remarks>
/// Per Constitution Article III (Test-First Imperative):
/// These tests are written BEFORE the fail-fast implementation exists.
/// Initial test run will FAIL - this is expected (TDD RED phase).
///
/// Test Strategy:
/// - Use IHost to simulate realistic application startup
/// - Verify OptionsValidationException thrown during host build/start
/// - Test both inline configuration and IConfiguration binding
/// - Validate that valid configurations succeed
/// - Ensure errors contain meaningful messages
/// </remarks>
public class ConfigurationValidationTests
{
    /// <summary>
    /// Tests that missing API key causes immediate startup failure when ValidateOnStart is enabled.
    /// </summary>
    [Fact]
    public void AddBreezSdk_WithMissingApiKey_ThrowsOnStartup()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBreezSdk(options =>
                {
                    options.ApiKey = null; // Invalid: Missing API key
                    options.Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
                    options.WorkingDirectory = @"C:\temp\breez-test";
                    options.Network = BreezNetwork.Testnet;
                });
            });

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
        {
            using var host = hostBuilder.Build();
            host.Start();
        });

        exception.OptionsType.Should().Be(typeof(BreezSdkOptions));
        exception.Failures.Should().Contain(f => f.Contains("ApiKey"));
    }

    /// <summary>
    /// Tests that missing mnemonic causes immediate startup failure.
    /// </summary>
    [Fact]
    public void AddBreezSdk_WithMissingMnemonic_ThrowsOnStartup()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBreezSdk(options =>
                {
                    options.ApiKey = "test-api-key-12345";
                    options.Mnemonic = ""; // Invalid: Empty mnemonic
                    options.WorkingDirectory = @"C:\temp\breez-test";
                    options.Network = BreezNetwork.Testnet;
                });
            });

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
        {
            using var host = hostBuilder.Build();
            host.Start();
        });

        exception.OptionsType.Should().Be(typeof(BreezSdkOptions));
        exception.Failures.Should().Contain(f => f.Contains("Mnemonic"));
    }

    /// <summary>
    /// Tests that missing working directory causes immediate startup failure.
    /// </summary>
    [Fact]
    public void AddBreezSdk_WithMissingWorkingDirectory_ThrowsOnStartup()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBreezSdk(options =>
                {
                    options.ApiKey = "test-api-key-12345";
                    options.Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
                    options.WorkingDirectory = null; // Invalid: Null working directory
                    options.Network = BreezNetwork.Testnet;
                });
            });

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
        {
            using var host = hostBuilder.Build();
            host.Start();
        });

        exception.OptionsType.Should().Be(typeof(BreezSdkOptions));
        exception.Failures.Should().Contain(f => f.Contains("WorkingDirectory"));
    }

    /// <summary>
    /// Tests that invalid network enum value causes immediate startup failure.
    /// </summary>
    [Fact]
    public void AddBreezSdk_WithInvalidNetwork_ThrowsOnStartup()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBreezSdk(options =>
                {
                    options.ApiKey = "test-api-key-12345";
                    options.Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
                    options.WorkingDirectory = @"C:\temp\breez-test";
                    options.Network = (BreezNetwork)999; // Invalid enum value
                });
            });

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
        {
            using var host = hostBuilder.Build();
            host.Start();
        });

        exception.OptionsType.Should().Be(typeof(BreezSdkOptions));
        exception.Failures.Should().Contain(f => f.Contains("Network"));
    }

    /// <summary>
    /// Tests that multiple validation errors are all reported at startup.
    /// </summary>
    [Fact]
    public void AddBreezSdk_WithMultipleErrors_ReportsAllFailures()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBreezSdk(options =>
                {
                    options.ApiKey = null; // Invalid
                    options.Mnemonic = ""; // Invalid
                    options.WorkingDirectory = "   "; // Invalid
                    options.Network = (BreezNetwork)999; // Invalid
                    options.MaxInvoiceAmountSat = 0; // Invalid
                    options.ConnectionTimeoutSeconds = -1; // Invalid
                });
            });

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
        {
            using var host = hostBuilder.Build();
            host.Start();
        });

        exception.OptionsType.Should().Be(typeof(BreezSdkOptions));
        exception.Failures.Should().HaveCountGreaterThan(3);
        exception.Failures.Should().Contain(f => f.Contains("ApiKey"));
        exception.Failures.Should().Contain(f => f.Contains("Mnemonic"));
        exception.Failures.Should().Contain(f => f.Contains("WorkingDirectory"));
    }

    /// <summary>
    /// Tests that valid configuration allows normal application startup.
    /// </summary>
    [Fact]
    public async Task AddBreezSdk_WithValidConfiguration_StartsSuccessfully()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBreezSdk(options =>
                {
                    options.ApiKey = "test-api-key-12345";
                    options.Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
                    options.WorkingDirectory = @"C:\temp\breez-test";
                    options.Network = BreezNetwork.Testnet;
                    options.MaxInvoiceAmountSat = 1000000;
                    options.ConnectionTimeoutSeconds = 30;
                });
            });

        // Act & Assert - Should not throw
        using var host = hostBuilder.Build();
        await host.StartAsync();
        await host.StopAsync();

        // Success - no exception thrown
    }

    /// <summary>
    /// Tests that configuration bound from IConfiguration fails fast on invalid values.
    /// </summary>
    [Fact]
    public void AddBreezSdk_WithInvalidConfigurationSection_ThrowsOnStartup()
    {
        // Arrange
        var configurationData = new Dictionary<string, string?>
        {
            ["BreezSdk:ApiKey"] = null, // Invalid
            ["BreezSdk:Mnemonic"] = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about",
            ["BreezSdk:WorkingDirectory"] = @"C:\temp\breez-test",
            ["BreezSdk:Network"] = "Testnet"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBreezSdk(configuration.GetSection("BreezSdk"));
            });

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
        {
            using var host = hostBuilder.Build();
            host.Start();
        });

        exception.OptionsType.Should().Be(typeof(BreezSdkOptions));
        exception.Failures.Should().Contain(f => f.Contains("ApiKey"));
    }

    /// <summary>
    /// Tests that valid IConfiguration section allows normal startup.
    /// </summary>
    [Fact]
    public async Task AddBreezSdk_WithValidConfigurationSection_StartsSuccessfully()
    {
        // Arrange
        var configurationData = new Dictionary<string, string?>
        {
            ["BreezSdk:ApiKey"] = "test-api-key-12345",
            ["BreezSdk:Mnemonic"] = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about",
            ["BreezSdk:WorkingDirectory"] = @"C:\temp\breez-test",
            ["BreezSdk:Network"] = "Testnet",
            ["BreezSdk:MaxInvoiceAmountSat"] = "1000000",
            ["BreezSdk:ConnectionTimeoutSeconds"] = "30"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBreezSdk(configuration.GetSection("BreezSdk"));
            });

        // Act & Assert - Should not throw
        using var host = hostBuilder.Build();
        await host.StartAsync();
        await host.StopAsync();

        // Success - no exception thrown
    }

    /// <summary>
    /// Tests that offline mode with missing API key still validates working directory.
    /// </summary>
    [Fact]
    public void AddBreezSdkOffline_WithMissingWorkingDirectory_ThrowsOnStartup()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBreezSdkOffline(options =>
                {
                    options.OfflineMode = true;
                    options.WorkingDirectory = null; // Invalid: Still required even in offline mode
                });
            });

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
        {
            using var host = hostBuilder.Build();
            host.Start();
        });

        exception.OptionsType.Should().Be(typeof(BreezSdkOptions));
        exception.Failures.Should().Contain(f => f.Contains("WorkingDirectory"));
    }

    /// <summary>
    /// Tests that offline mode with valid configuration starts successfully.
    /// </summary>
    [Fact]
    public async Task AddBreezSdkOffline_WithValidConfiguration_StartsSuccessfully()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBreezSdkOffline(options =>
                {
                    options.OfflineMode = true;
                    options.WorkingDirectory = @"C:\temp\breez-test";
                    options.OfflineMockBalanceSat = 100000;
                });
            });

        // Act & Assert - Should not throw
        using var host = hostBuilder.Build();
        await host.StartAsync();
        await host.StopAsync();

        // Success - no exception thrown
    }

    /// <summary>
    /// Tests that validation happens before any service is resolved (fail-fast principle).
    /// This ensures the application doesn't start with invalid configuration.
    /// </summary>
    [Fact]
    public void AddBreezSdk_ValidationFailure_OccursBeforeServiceResolution()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBreezSdk(options =>
                {
                    options.ApiKey = null; // Invalid
                    options.Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
                    options.WorkingDirectory = @"C:\temp\breez-test";
                });
            });

        // Act & Assert
        // The exception should be thrown during Build() or Start(), not when resolving services
        var exception = Assert.Throws<OptionsValidationException>(() =>
        {
            using var host = hostBuilder.Build();
            host.Start();
        });

        exception.Should().NotBeNull();
        exception.OptionsType.Should().Be(typeof(BreezSdkOptions));
    }

    /// <summary>
    /// Tests that invalid circuit breaker configuration causes startup failure.
    /// </summary>
    [Fact]
    public void AddBreezSdk_WithInvalidCircuitBreaker_ThrowsOnStartup()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBreezSdk(options =>
                {
                    options.ApiKey = "test-api-key-12345";
                    options.Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
                    options.WorkingDirectory = @"C:\temp\breez-test";
                    options.Network = BreezNetwork.Testnet;
                    options.CircuitBreaker = new CircuitBreakerOptions
                    {
                        FailureThreshold = 0, // Invalid
                        SamplingDurationSeconds = -1, // Invalid
                        BreakDurationSeconds = 0 // Invalid
                    };
                });
            });

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
        {
            using var host = hostBuilder.Build();
            host.Start();
        });

        exception.OptionsType.Should().Be(typeof(BreezSdkOptions));
        exception.Failures.Should().Contain(f => f.Contains("CircuitBreaker"));
    }

    /// <summary>
    /// Tests that invalid webhook URL causes startup failure.
    /// </summary>
    [Fact]
    public void AddBreezSdk_WithInvalidWebhookUrl_ThrowsOnStartup()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBreezSdk(options =>
                {
                    options.ApiKey = "test-api-key-12345";
                    options.Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
                    options.WorkingDirectory = @"C:\temp\breez-test";
                    options.Network = BreezNetwork.Testnet;
                    options.WebhookUrl = "not-a-valid-url"; // Invalid URL
                });
            });

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
        {
            using var host = hostBuilder.Build();
            host.Start();
        });

        exception.OptionsType.Should().Be(typeof(BreezSdkOptions));
        exception.Failures.Should().Contain(f => f.Contains("WebhookUrl"));
    }

    /// <summary>
    /// Tests that zero max invoice amount causes startup failure.
    /// </summary>
    [Fact]
    public void AddBreezSdk_WithZeroMaxInvoiceAmount_ThrowsOnStartup()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBreezSdk(options =>
                {
                    options.ApiKey = "test-api-key-12345";
                    options.Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
                    options.WorkingDirectory = @"C:\temp\breez-test";
                    options.Network = BreezNetwork.Testnet;
                    options.MaxInvoiceAmountSat = 0; // Invalid
                });
            });

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
        {
            using var host = hostBuilder.Build();
            host.Start();
        });

        exception.OptionsType.Should().Be(typeof(BreezSdkOptions));
        exception.Failures.Should().Contain(f => f.Contains("MaxInvoiceAmountSat"));
    }

    /// <summary>
    /// Tests that negative connection timeout causes startup failure.
    /// </summary>
    [Fact]
    public void AddBreezSdk_WithNegativeConnectionTimeout_ThrowsOnStartup()
    {
        // Arrange
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddBreezSdk(options =>
                {
                    options.ApiKey = "test-api-key-12345";
                    options.Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
                    options.WorkingDirectory = @"C:\temp\breez-test";
                    options.Network = BreezNetwork.Testnet;
                    options.ConnectionTimeoutSeconds = -1; // Invalid
                });
            });

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(() =>
        {
            using var host = hostBuilder.Build();
            host.Start();
        });

        exception.OptionsType.Should().Be(typeof(BreezSdkOptions));
        exception.Failures.Should().Contain(f => f.Contains("ConnectionTimeoutSeconds"));
    }
}
