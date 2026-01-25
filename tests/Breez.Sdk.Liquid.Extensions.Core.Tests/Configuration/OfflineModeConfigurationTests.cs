using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Breez.Sdk.Liquid.Extensions.Core.Extensions;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Infrastructure;
using Breez.Sdk.Liquid.Extensions.Core.Persistence;
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using FluentAssertions;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Configuration;

/// <summary>
/// Unit tests for offline mode configuration in ServiceCollectionExtensions.
/// Tests the specific requirements and behavior of AddBreezSdkOffline methods.
/// </summary>
/// <remarks>
/// This test suite verifies the TDD RED phase - some tests may fail initially
/// if offline mode validation behavior is not fully implemented.
///
/// Test Categories:
/// 1. Offline Mode Registration - Verifies correct service registration
/// 2. Options Validation - Tests ApiKey/Mnemonic requirements in offline mode
/// 3. Simulation Options - Tests offline simulation configuration
/// 4. Mode Switching - Tests switching between online and offline modes
/// </remarks>
public class OfflineModeConfigurationTests
{
    #region Offline Mode Service Registration Tests

    [Fact]
    public void AddBreezSdkOffline_RegistersOfflineBreezSdkService_NotBreezSdkService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true; // Explicitly set offline mode
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IBreezSdkService>();

        service.Should().NotBeNull();
        service.Should().BeOfType<OfflineBreezSdkService>("offline mode should register OfflineBreezSdkService");
    }

    [Fact]
    public void AddBreezSdkOffline_DoesNotRegisterIBreezSdkWrapper()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var wrapper = provider.GetService<IBreezSdkWrapper>();

        wrapper.Should().BeNull("offline mode should NOT register IBreezSdkWrapper since it doesn't interact with native SDK");
    }

    [Fact]
    public void AddBreezSdkOffline_RegistersIBreezSdkService_AsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var service1 = provider.GetService<IBreezSdkService>();
        var service2 = provider.GetService<IBreezSdkService>();

        service1.Should().NotBeNull();
        service1.Should().BeSameAs(service2, "offline service should be registered as singleton");
    }

    [Fact]
    public void AddBreezSdkOffline_RegistersIPaymentRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var repo = provider.GetService<IPaymentRepository>();

        repo.Should().NotBeNull();
        repo.Should().BeOfType<InMemoryPaymentRepository>("offline mode should register default repository");
    }

    #endregion

    #region Offline Mode Validation - ApiKey and Mnemonic NOT Required

    [Fact]
    public void AddBreezSdkOffline_DoesNotRequireApiKey()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
            // ApiKey intentionally not set
            options.Mnemonic = null; // Explicitly null
        });

        // Assert - Should NOT throw validation exception
        var provider = services.BuildServiceProvider();
        var action = () => provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        // In offline mode, ApiKey and Mnemonic should be optional
        action.Should().NotThrow<OptionsValidationException>("offline mode should not require ApiKey");
    }

    [Fact]
    public void AddBreezSdkOffline_DoesNotRequireMnemonic()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
            options.ApiKey = null; // Explicitly null
            // Mnemonic intentionally not set
        });

        // Assert - Should NOT throw validation exception
        var provider = services.BuildServiceProvider();
        var action = () => provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        // In offline mode, Mnemonic should be optional
        action.Should().NotThrow<OptionsValidationException>("offline mode should not require Mnemonic");
    }

    [Fact]
    public void AddBreezSdkOffline_AllowsEmptyApiKeyAndMnemonic()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
            options.ApiKey = string.Empty;
            options.Mnemonic = string.Empty;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var action = () => provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        action.Should().NotThrow<OptionsValidationException>("offline mode should allow empty credentials");
    }

    #endregion

    #region Offline Mode Validation - WorkingDirectory Still Required

    [Fact]
    public void AddBreezSdkOffline_RequiresWorkingDirectory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
            options.WorkingDirectory = null; // Intentionally null
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var action = () => provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        action.Should().Throw<OptionsValidationException>()
            .WithMessage("*WorkingDirectory*required*", "WorkingDirectory is required even in offline mode");
    }

    [Fact]
    public void AddBreezSdkOffline_RequiresNonEmptyWorkingDirectory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
            options.WorkingDirectory = "   "; // Whitespace only
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var action = () => provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        action.Should().Throw<OptionsValidationException>()
            .WithMessage("*WorkingDirectory*", "WorkingDirectory cannot be empty or whitespace");
    }

    [Fact]
    public void AddBreezSdkOffline_WithValidWorkingDirectory_Succeeds()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedWorkingDir = @"C:\temp\breez-offline";

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
            options.WorkingDirectory = expectedWorkingDir;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var actualOptions = provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        actualOptions.WorkingDirectory.Should().Be(expectedWorkingDir);
    }

    #endregion

    #region Offline Simulation Options Tests

    [Fact]
    public void AddBreezSdkOffline_HonorsOfflineMockBalanceSat()
    {
        // Arrange
        var services = new ServiceCollection();
        const ulong expectedBalance = 250_000ul;

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
            options.OfflineMockBalanceSat = expectedBalance;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var actualOptions = provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        actualOptions.OfflineMockBalanceSat.Should().Be(expectedBalance, "offline mode should honor configured mock balance");
    }

    [Fact]
    public void AddBreezSdkOffline_DefaultMockBalanceSat_Is100000()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
            // Don't set OfflineMockBalanceSat - use default
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var actualOptions = provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        actualOptions.OfflineMockBalanceSat.Should().Be(100_000ul, "default mock balance should be 100,000 sats");
    }

    [Fact]
    public void AddBreezSdkOffline_HonorsOfflineSimulateDelayMs()
    {
        // Arrange
        var services = new ServiceCollection();
        const int expectedDelayMs = 500;

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
            options.OfflineSimulateDelayMs = expectedDelayMs;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var actualOptions = provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        actualOptions.OfflineSimulateDelayMs.Should().Be(expectedDelayMs, "offline mode should honor configured delay");
    }

    [Fact]
    public void AddBreezSdkOffline_DefaultSimulateDelayMs_IsZero()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
            // Don't set OfflineSimulateDelayMs - use default
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var actualOptions = provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        actualOptions.OfflineSimulateDelayMs.Should().Be(0, "default delay should be 0 ms (no delay)");
    }

    [Fact]
    public void AddBreezSdkOffline_HonorsOfflineSimulateFailureRate()
    {
        // Arrange
        var services = new ServiceCollection();
        const double expectedFailureRate = 0.25; // 25% failure rate

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
            options.OfflineSimulateFailureRate = expectedFailureRate;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var actualOptions = provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        actualOptions.OfflineSimulateFailureRate.Should().Be(expectedFailureRate, "offline mode should honor configured failure rate");
    }

    [Fact]
    public void AddBreezSdkOffline_DefaultSimulateFailureRate_IsZero()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
            // Don't set OfflineSimulateFailureRate - use default
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var actualOptions = provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        actualOptions.OfflineSimulateFailureRate.Should().Be(0.0, "default failure rate should be 0.0 (no failures)");
    }

    [Fact]
    public void AddBreezSdkOffline_AllowsMaximumFailureRate()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
            options.OfflineSimulateFailureRate = 1.0; // 100% failure rate
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var actualOptions = provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        actualOptions.OfflineSimulateFailureRate.Should().Be(1.0, "should allow 100% failure rate for testing");
    }

    [Fact]
    public void AddBreezSdkOffline_WithAllSimulationOptions_ConfiguresCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        const ulong expectedBalance = 500_000ul;
        const int expectedDelay = 1000;
        const double expectedFailureRate = 0.15;

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
            options.OfflineMockBalanceSat = expectedBalance;
            options.OfflineSimulateDelayMs = expectedDelay;
            options.OfflineSimulateFailureRate = expectedFailureRate;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var actualOptions = provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        actualOptions.OfflineMockBalanceSat.Should().Be(expectedBalance);
        actualOptions.OfflineSimulateDelayMs.Should().Be(expectedDelay);
        actualOptions.OfflineSimulateFailureRate.Should().Be(expectedFailureRate);
    }

    #endregion

    #region Mode Switching Tests

    [Fact]
    public void SwitchingFromOnlineToOfflineMode_ChangesServiceRegistration()
    {
        // Arrange
        var services1 = new ServiceCollection();
        var services2 = new ServiceCollection();

        // Act - Register online mode first
        services1.AddBreezSdk(options =>
        {
            options.ApiKey = "test-api-key";
            options.Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
            options.OfflineMode = false;
        });

        // Then register offline mode in separate collection
        services2.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
        });

        // Assert
        var onlineProvider = services1.BuildServiceProvider();
        var offlineProvider = services2.BuildServiceProvider();

        var onlineService = onlineProvider.GetService<IBreezSdkService>();
        var offlineService = offlineProvider.GetService<IBreezSdkService>();

        onlineService.Should().BeOfType<BreezSdkService>("online mode should use BreezSdkService");
        offlineService.Should().BeOfType<OfflineBreezSdkService>("offline mode should use OfflineBreezSdkService");
    }

    [Fact]
    public void SwitchingFromOnlineToOfflineMode_OnlineRegistersWrapper_OfflineDoesNot()
    {
        // Arrange
        var services1 = new ServiceCollection();
        var services2 = new ServiceCollection();

        // Act
        services1.AddBreezSdk(options =>
        {
            options.ApiKey = "test-api-key";
            options.Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        });

        services2.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
        });

        // Assert
        var onlineProvider = services1.BuildServiceProvider();
        var offlineProvider = services2.BuildServiceProvider();

        var onlineWrapper = onlineProvider.GetService<IBreezSdkWrapper>();
        var offlineWrapper = offlineProvider.GetService<IBreezSdkWrapper>();

        onlineWrapper.Should().NotBeNull("online mode should register IBreezSdkWrapper");
        offlineWrapper.Should().BeNull("offline mode should NOT register IBreezSdkWrapper");
    }

    [Fact]
    public void AddBreezSdk_WithOfflineModeTrue_StillRequiresApiKeyAndMnemonic()
    {
        // Arrange - Using AddBreezSdk (not AddBreezSdkOffline) with OfflineMode=true
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdk(options =>
        {
            options.OfflineMode = true; // Set flag to true
            // But don't provide ApiKey or Mnemonic
        });

        // Assert - AddBreezSdk should STILL require credentials even if OfflineMode flag is set
        // because the validator checks the OfflineMode flag
        var provider = services.BuildServiceProvider();
        var action = () => provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        // This should NOT throw because OfflineMode=true makes credentials optional in the validator
        action.Should().NotThrow<OptionsValidationException>(
            "when OfflineMode flag is true, credentials should be optional regardless of registration method");
    }

    [Fact]
    public void AddBreezSdkOffline_WithConfigurationSection_SetsOfflineModeFlag()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            ["BreezSdk:OfflineMode"] = "true",
            ["BreezSdk:OfflineMockBalanceSat"] = "200000",
            ["BreezSdk:OfflineSimulateDelayMs"] = "250"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddBreezSdkOffline(configuration.GetSection("BreezSdk"));

        // Assert
        var provider = services.BuildServiceProvider();
        var actualOptions = provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        actualOptions.OfflineMode.Should().BeTrue("configuration should set OfflineMode flag");
        actualOptions.OfflineMockBalanceSat.Should().Be(200_000ul);
        actualOptions.OfflineSimulateDelayMs.Should().Be(250);
    }

    #endregion

    #region Configuration Section Tests

    [Fact]
    public void AddBreezSdkOffline_WithConfigurationSection_DoesNotRequireCredentials()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            ["BreezSdk:OfflineMode"] = "true",
            ["BreezSdk:OfflineMockBalanceSat"] = "150000"
            // ApiKey and Mnemonic intentionally missing
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddBreezSdkOffline(configuration.GetSection("BreezSdk"));

        // Assert
        var provider = services.BuildServiceProvider();
        var action = () => provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        action.Should().NotThrow<OptionsValidationException>("offline mode should not require credentials from configuration");
    }

    [Fact]
    public void AddBreezSdkOffline_WithConfigurationSection_CanStillProvideOptionalCredentials()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            ["BreezSdk:OfflineMode"] = "true",
            ["BreezSdk:ApiKey"] = "optional-key",
            ["BreezSdk:Mnemonic"] = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddBreezSdkOffline(configuration.GetSection("BreezSdk"));

        // Assert
        var provider = services.BuildServiceProvider();
        var actualOptions = provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        actualOptions.ApiKey.Should().Be("optional-key", "offline mode can still accept optional credentials");
        actualOptions.Mnemonic.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Argument Validation Tests

    [Fact]
    public void AddBreezSdkOffline_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var action = () => services!.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
        });

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddBreezSdkOffline_ThrowsArgumentNullException_WhenConfigureOptionsIsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<BreezSdkOptions>? configureOptions = null;

        // Act
        var action = () => services.AddBreezSdkOffline(configureOptions!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configureOptions");
    }

    [Fact]
    public void AddBreezSdkOffline_ThrowsArgumentNullException_WhenConfigurationSectionIsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfigurationSection? configSection = null;

        // Act
        var action = () => services.AddBreezSdkOffline(configSection!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configurationSection");
    }

    #endregion

    #region Integration with Repository Tests

    [Fact]
    public void AddBreezSdkOffline_AllowsCustomRepositoryOverride()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IPaymentRepository, CustomOfflineTestRepository>();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.OfflineMode = true;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var repo = provider.GetService<IPaymentRepository>();

        repo.Should().NotBeNull();
        repo.Should().BeOfType<CustomOfflineTestRepository>("custom repository should be used when registered first");
    }

    #endregion

    #region Helper Classes

    private class CustomOfflineTestRepository : IPaymentRepository
    {
        public Task<PaymentState?> GetByHashAsync(string paymentHash, CancellationToken cancellationToken = default)
            => Task.FromResult<PaymentState?>(null);

        public Task<IReadOnlyList<PaymentState>> GetByStatusAsync(
            PaymentStatus status,
            int limit = 100,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PaymentState>>(Array.Empty<PaymentState>());

        public Task<PaymentState> AddAsync(PaymentState payment, CancellationToken cancellationToken = default)
            => Task.FromResult(payment);

        public Task<PaymentState> UpdateAsync(PaymentState payment, CancellationToken cancellationToken = default)
            => Task.FromResult(payment);

        public Task<bool> ExistsAsync(string paymentHash, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<IReadOnlyList<PaymentState>> GetAllAsync(
            int offset = 0,
            int limit = 50,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PaymentState>>(Array.Empty<PaymentState>());

        public Task<IReadOnlyList<PaymentState>> GetByDateRangeAsync(
            DateTimeOffset from,
            DateTimeOffset to,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PaymentState>>(Array.Empty<PaymentState>());
    }

    #endregion
}
