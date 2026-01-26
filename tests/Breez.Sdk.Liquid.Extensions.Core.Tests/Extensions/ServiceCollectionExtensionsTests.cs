using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Breez.Sdk.Liquid.Extensions.Core.Extensions;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Infrastructure;
using Breez.Sdk.Liquid.Extensions.Core.Persistence;
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using FluentAssertions;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Extensions;

/// <summary>
/// Unit tests for ServiceCollectionExtensions.
/// Tests the dependency injection registration patterns for BreezSDK services.
/// </summary>
/// <remarks>
/// This is the TDD RED phase - these tests reference classes that DON'T EXIST YET.
/// Expected failures until implementation is complete.
/// </remarks>
public class ServiceCollectionExtensionsTests
{
    #region AddBreezSdk Core Registration Tests

    [Fact]
    public void AddBreezSdk_RegistersIBreezSdkService_AsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdk(options =>
        {
            options.ApiKey = "test-api-key";
            options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var service1 = provider.GetService<IBreezSdkService>();
        var service2 = provider.GetService<IBreezSdkService>();

        service1.Should().NotBeNull();
        service1.Should().BeSameAs(service2, "service should be registered as singleton");
    }

    [Fact]
    public void AddBreezSdk_RegistersIBreezSdkWrapper_AsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdk(options =>
        {
            options.ApiKey = "test-api-key";
            options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var wrapper1 = provider.GetService<IBreezSdkWrapper>();
        var wrapper2 = provider.GetService<IBreezSdkWrapper>();

        wrapper1.Should().NotBeNull();
        wrapper1.Should().BeSameAs(wrapper2, "wrapper should be registered as singleton");
    }

    [Fact]
    public void AddBreezSdk_RegistersIPaymentRepository_AsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdk(options =>
        {
            options.ApiKey = "test-api-key";
            options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var repo1 = provider.GetService<IPaymentRepository>();
        var repo2 = provider.GetService<IPaymentRepository>();

        repo1.Should().NotBeNull();
        repo1.Should().BeOfType<InMemoryPaymentRepository>();
        repo1.Should().BeSameAs(repo2, "repository should be registered as singleton");
    }

    [Fact]
    public void AddBreezSdk_RegistersConcreteBreezSdkService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdk(options =>
        {
            options.ApiKey = "test-api-key";
            options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IBreezSdkService>();

        service.Should().NotBeNull();
        service.Should().BeOfType<BreezSdkService>();
    }

    [Fact]
    public void AddBreezSdk_RegistersConcreteBreezSdkWrapper()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdk(options =>
        {
            options.ApiKey = "test-api-key";
            options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var wrapper = provider.GetService<IBreezSdkWrapper>();

        wrapper.Should().NotBeNull();
        wrapper.Should().BeOfType<BreezSdkWrapper>();
    }

    #endregion

    #region Options Configuration Tests

    [Fact]
    public void AddBreezSdk_WithActionDelegate_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        const string expectedApiKey = "my-api-key";
        const string expectedMnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        const string expectedWorkingDir = "C:\\breez-data";

        // Act
        services.AddBreezSdk(options =>
        {
            options.ApiKey = expectedApiKey;
            options.Mnemonic = expectedMnemonic;
            options.WorkingDirectory = expectedWorkingDir;
            options.Network = BreezNetwork.Testnet;
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var optionsSnapshot = provider.GetRequiredService<IOptions<BreezSdkOptions>>();
        var actualOptions = optionsSnapshot.Value;

        actualOptions.ApiKey.Should().Be(expectedApiKey);
        actualOptions.Mnemonic.Should().Be(expectedMnemonic);
        actualOptions.WorkingDirectory.Should().Be(expectedWorkingDir);
        actualOptions.Network.Should().Be(BreezNetwork.Testnet);
    }

    [Fact]
    public void AddBreezSdk_WithConfigurationSection_BindsOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            ["BreezSdk:ApiKey"] = "config-api-key",
            ["BreezSdk:Mnemonic"] = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about",
            ["BreezSdk:WorkingDirectory"] = "C:\\breez-config-data",
            ["BreezSdk:Network"] = "Mainnet"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddBreezSdk(configuration.GetSection("BreezSdk"));

        // Assert
        var provider = services.BuildServiceProvider();
        var optionsSnapshot = provider.GetRequiredService<IOptions<BreezSdkOptions>>();
        var actualOptions = optionsSnapshot.Value;

        actualOptions.ApiKey.Should().Be("config-api-key");
        actualOptions.Mnemonic.Should().Be("abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about");
        actualOptions.WorkingDirectory.Should().Be("C:\\breez-config-data");
        actualOptions.Network.Should().Be(BreezNetwork.Mainnet);
    }

    [Fact]
    public void AddBreezSdk_RegistersOptionsWithIOptionsPattern()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdk(options =>
        {
            options.ApiKey = "test-key";
            options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetService<IOptionsMonitor<BreezSdkOptions>>();
        var optionsSnapshot = provider.GetService<IOptionsSnapshot<BreezSdkOptions>>();

        optionsMonitor.Should().NotBeNull("IOptionsMonitor<T> should be available");
        optionsSnapshot.Should().NotBeNull("IOptionsSnapshot<T> should be available");
    }

    #endregion

    #region Options Validation Tests

    [Fact]
    public void AddBreezSdk_ValidatesOptionsOnStartup_WhenApiKeyMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBreezSdk(options =>
        {
            options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
            // ApiKey intentionally missing
        });

        // Act
        var provider = services.BuildServiceProvider();
        var action = () => provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        // Assert
        action.Should().Throw<OptionsValidationException>()
            .WithMessage("*ApiKey*");
    }

    [Fact]
    public void AddBreezSdk_ValidatesOptionsOnStartup_WhenMnemonicMissing()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBreezSdk(options =>
        {
            options.ApiKey = "test-api-key";
            // Mnemonic intentionally missing
        });

        // Act
        var provider = services.BuildServiceProvider();
        var action = () => provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        // Assert
        action.Should().Throw<OptionsValidationException>()
            .WithMessage("*Mnemonic*");
    }

    [Fact]
    public void AddBreezSdk_ValidatesOptionsOnStartup_WhenMnemonicInvalid()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBreezSdk(options =>
        {
            options.ApiKey = "test-api-key";
            options.Mnemonic = "too short"; // Invalid mnemonic
        });

        // Act
        var provider = services.BuildServiceProvider();
        var action = () => provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;

        // Assert
        action.Should().Throw<OptionsValidationException>()
            .WithMessage("*Mnemonic*12*words*");
    }

    #endregion

    #region AddBreezSdkOffline Tests

    [Fact]
    public void AddBreezSdkOffline_RegistersOfflineBreezSdkService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.ApiKey = "test-api-key";
            options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IBreezSdkService>();

        service.Should().NotBeNull();
        service.Should().BeOfType<OfflineBreezSdkService>();
    }

    [Fact]
    public void AddBreezSdkOffline_RegistersIBreezSdkService_AsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.ApiKey = "test-api-key";
            options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var service1 = provider.GetService<IBreezSdkService>();
        var service2 = provider.GetService<IBreezSdkService>();

        service1.Should().NotBeNull();
        service1.Should().BeSameAs(service2, "offline service should be registered as singleton");
    }

    [Fact]
    public void AddBreezSdkOffline_DoesNotRegisterIBreezSdkWrapper()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.ApiKey = "test-api-key";
            options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var wrapper = provider.GetService<IBreezSdkWrapper>();

        wrapper.Should().BeNull("offline mode should not register SDK wrapper");
    }

    [Fact]
    public void AddBreezSdkOffline_RegistersInMemoryPaymentRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdkOffline(options =>
        {
            options.ApiKey = "test-api-key";
            options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var repo = provider.GetService<IPaymentRepository>();

        repo.Should().NotBeNull();
        repo.Should().BeOfType<InMemoryPaymentRepository>();
    }

    [Fact]
    public void AddBreezSdkOffline_WithConfigurationSection_BindsOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            ["BreezSdk:ApiKey"] = "offline-api-key",
            ["BreezSdk:Mnemonic"] = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        services.AddBreezSdkOffline(configuration.GetSection("BreezSdk"));

        // Assert
        var provider = services.BuildServiceProvider();
        var optionsSnapshot = provider.GetRequiredService<IOptions<BreezSdkOptions>>();
        var actualOptions = optionsSnapshot.Value;

        actualOptions.ApiKey.Should().Be("offline-api-key");
        actualOptions.Mnemonic.Should().Be("abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about");
    }

    #endregion

    #region Argument Validation Tests

    [Fact]
    public void AddBreezSdk_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var action = () => services!.AddBreezSdk(options =>
        {
            options.ApiKey = "test-key";
            options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
        });

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddBreezSdk_ThrowsArgumentNullException_WhenConfigureOptionsIsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        Action<BreezSdkOptions>? configureOptions = null;

        // Act
        var action = () => services.AddBreezSdk(configureOptions!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configureOptions");
    }

    [Fact]
    public void AddBreezSdk_ThrowsArgumentNullException_WhenConfigurationSectionIsNull()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfigurationSection? configSection = null;

        // Act
        var action = () => services.AddBreezSdk(configSection!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configurationSection");
    }

    [Fact]
    public void AddBreezSdkOffline_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var action = () => services!.AddBreezSdkOffline(options =>
        {
            options.ApiKey = "test-key";
            options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
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

    #endregion

    #region Multiple Registration Tests

    [Fact]
    public void AddBreezSdk_CalledTwice_DoesNotDuplicateServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBreezSdk(options =>
        {
            options.ApiKey = "test-key-1";
            options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
        });
        services.AddBreezSdk(options =>
        {
            options.ApiKey = "test-key-2";
            options.Mnemonic = "different mnemonic phrase with twelve secret words here for testing development purposes";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var serviceDescriptors = services.Where(sd => sd.ServiceType == typeof(IBreezSdkService)).ToList();

        serviceDescriptors.Should().HaveCount(2, "both registrations should be present");

        // Verify last registration wins (standard DI behavior)
        var options = provider.GetRequiredService<IOptions<BreezSdkOptions>>().Value;
        options.ApiKey.Should().Be("test-key-2", "last registered options should win");
    }

    [Fact]
    public void AddBreezSdk_CanBeChainedWithOtherServiceRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services
            .AddLogging()
            .AddBreezSdk(options =>
            {
                options.ApiKey = "test-key";
                options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
            })
            .AddSingleton<ITestService, TestService>();

        // Assert
        var provider = services.BuildServiceProvider();

        provider.GetService<IBreezSdkService>().Should().NotBeNull();
        provider.GetService<ITestService>().Should().NotBeNull();
    }

    #endregion

    #region Custom Repository Tests

    [Fact]
    public void AddBreezSdk_AllowsCustomRepositoryOverride()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IPaymentRepository, CustomTestRepository>();

        // Act
        services.AddBreezSdk(options =>
        {
            options.ApiKey = "test-key";
            options.Mnemonic = "test mnemonic phrase with twelve secret words here for testing development purposes";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var repo = provider.GetService<IPaymentRepository>();

        repo.Should().NotBeNull();
        // Note: DI behavior - first registration wins for GetService
        repo.Should().BeOfType<CustomTestRepository>("custom repository should be used when registered first");
    }

    #endregion

    #region Helper Classes

    private interface ITestService { }
    private class TestService : ITestService { }

    private class CustomTestRepository : IPaymentRepository
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
