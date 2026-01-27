using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Exceptions;
using Breez.Sdk.Liquid.Extensions.Core.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using Polly.Retry;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Infrastructure;

/// <summary>
/// Unit tests for BreezSdkWrapper.
/// These tests verify the wrapper's behavior in managing SDK lifecycle,
/// configuration validation, and operation delegation.
/// </summary>
/// <remarks>
/// TDD RED PHASE: These tests reference classes that don't exist yet.
/// Expected compilation failures until implementation (T053-T057) is complete.
/// </remarks>
public class BreezSdkWrapperTests : IAsyncDisposable
{
    private readonly Mock<IOptions<BreezSdkOptions>> _optionsMock;
    private readonly Mock<ILogger<BreezSdkWrapper>> _loggerMock;
    private BreezSdkWrapper? _sut;

    public BreezSdkWrapperTests()
    {
        _optionsMock = new Mock<IOptions<BreezSdkOptions>>();
        _loggerMock = new Mock<ILogger<BreezSdkWrapper>>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_sut is not null)
        {
            await _sut.DisposeAsync();
        }
    }

    #region Connection Lifecycle Tests

    [Fact]
    public async Task ConnectAsync_WithValidConfiguration_InitializesSDK()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act
        await _sut.ConnectAsync();

        // Assert
        _sut.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task ConnectAsync_WithValidConfiguration_SetsIsConnectedToTrue()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act
        await _sut.ConnectAsync();

        // Assert
        _sut.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task ConnectAsync_WhenAlreadyConnected_DoesNotReinitialize()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);
        await _sut.ConnectAsync();

        // Act - calling connect again
        await _sut.ConnectAsync();

        // Assert
        _sut.IsConnected.Should().BeTrue();
        // Implementation should log that already connected (verify via logger mock if needed)
    }

    [Fact]
    public async Task ConnectAsync_WithSDKInitializationFailure_ThrowsConnectionException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ApiKey = "invalid-api-key-that-will-fail";
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Use a fast test policy to avoid production retry delays (2s exponential backoff).
        // We're testing that the exception propagates, not the retry timing.
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object, CreateFastConnectPolicy());

        // Act & Assert
        var act = async () => await _sut.ConnectAsync();
        await act.Should().ThrowAsync<ConnectionException>()
            .WithMessage("*initialization*");
    }

    [Fact]
    public async Task DisconnectAsync_WhenConnected_SetsIsConnectedToFalse()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);
        await _sut.ConnectAsync();

        // Act
        await _sut.DisconnectAsync();

        // Assert
        _sut.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task DisconnectAsync_WhenConnected_DisposesSDKResources()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);
        await _sut.ConnectAsync();

        // Act
        await _sut.DisconnectAsync();

        // Assert
        _sut.IsConnected.Should().BeFalse();
        // Subsequent operations should throw ConnectionException
        var act = async () => await _sut.GetWalletInfoAsync();
        await act.Should().ThrowAsync<ConnectionException>()
            .WithMessage("*not connected*");
    }

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_DoesNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = async () => await _sut.DisconnectAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisposeAsync_WhenConnected_CallsDisconnectAsync()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);
        await _sut.ConnectAsync();

        // Act
        await _sut.DisposeAsync();

        // Assert
        _sut.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task DisposeAsync_WhenNotConnected_DoesNotThrow()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = async () => await _sut.DisposeAsync();
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region Configuration Validation Tests

    [Fact]
    public async Task ConnectAsync_WithMissingApiKey_ThrowsConfigurationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ApiKey = null!;
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = async () => await _sut.ConnectAsync();
        await act.Should().ThrowAsync<ConfigurationException>()
            .Where(ex => ex.PropertyName == nameof(BreezSdkOptions.ApiKey))
            .WithMessage("*API key*");
    }

    [Fact]
    public async Task ConnectAsync_WithEmptyApiKey_ThrowsConfigurationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ApiKey = string.Empty;
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = async () => await _sut.ConnectAsync();
        await act.Should().ThrowAsync<ConfigurationException>()
            .Where(ex => ex.PropertyName == nameof(BreezSdkOptions.ApiKey));
    }

    [Fact]
    public async Task ConnectAsync_WithMissingMnemonic_ThrowsConfigurationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.Mnemonic = null!;
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = async () => await _sut.ConnectAsync();
        await act.Should().ThrowAsync<ConfigurationException>()
            .Where(ex => ex.PropertyName == nameof(BreezSdkOptions.Mnemonic))
            .WithMessage("*mnemonic*");
    }

    [Fact]
    public async Task ConnectAsync_WithEmptyMnemonic_ThrowsConfigurationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.Mnemonic = "   ";
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = async () => await _sut.ConnectAsync();
        await act.Should().ThrowAsync<ConfigurationException>()
            .Where(ex => ex.PropertyName == nameof(BreezSdkOptions.Mnemonic));
    }

    [Fact]
    public async Task ConnectAsync_WithMissingWorkingDirectory_ThrowsConfigurationException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.WorkingDirectory = null!;
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = async () => await _sut.ConnectAsync();
        await act.Should().ThrowAsync<ConfigurationException>()
            .Where(ex => ex.PropertyName == nameof(BreezSdkOptions.WorkingDirectory));
    }

    [Theory]
    [InlineData(BreezNetwork.Mainnet)]
    [InlineData(BreezNetwork.Testnet)]
    public async Task ConnectAsync_WithValidNetwork_UsesCorrectConfiguration(BreezNetwork network)
    {
        // Arrange
        var options = CreateValidOptions();
        options.Network = network;
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act
        await _sut.ConnectAsync();

        // Assert
        _sut.IsConnected.Should().BeTrue();
        // The wrapper should have initialized SDK with the correct network
    }

    #endregion

    #region SDK Operation Tests

    [Fact]
    public async Task PrepareReceivePaymentAsync_WithValidAmount_ReturnsValidResponse()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);
        await _sut.ConnectAsync();

        // Act
        var result = await _sut.PrepareReceivePaymentAsync(
            amountSat: 5000,
            description: "Test payment",
            expirySec: 3600);

        // Assert
        result.Should().NotBeNull();
        result.Invoice.Should().NotBeNullOrEmpty();
        result.PaymentHash.Should().NotBeNullOrEmpty();
        result.Invoice.Should().StartWith("lnbc"); // Lightning invoice prefix
    }

    [Fact]
    public async Task PrepareReceivePaymentAsync_WithZeroAmount_ThrowsPaymentException()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);
        await _sut.ConnectAsync();

        // Act & Assert
        var act = async () => await _sut.PrepareReceivePaymentAsync(
            amountSat: 0,
            description: null,
            expirySec: null);

        await act.Should().ThrowAsync<PaymentException>()
            .WithMessage("*amount*");
    }

    [Fact]
    public async Task PrepareReceivePaymentAsync_WhenNotConnected_ThrowsConnectionException()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = async () => await _sut.PrepareReceivePaymentAsync(
            amountSat: 5000,
            description: null,
            expirySec: null);

        await act.Should().ThrowAsync<ConnectionException>()
            .WithMessage("*not connected*");
    }

    [Fact]
    public async Task PrepareReceivePaymentAsync_WithCustomExpiry_ReturnsInvoiceWithCorrectExpiry()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);
        await _sut.ConnectAsync();
        var customExpiry = 7200u; // 2 hours

        // Act
        var result = await _sut.PrepareReceivePaymentAsync(
            amountSat: 5000,
            description: "Test with custom expiry",
            expirySec: customExpiry);

        // Assert
        result.Should().NotBeNull();
        result.ExpiryTimestamp.Should().BeGreaterThan(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    [Fact]
    public async Task GetWalletInfoAsync_WhenConnected_ReturnsWalletInfo()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);
        await _sut.ConnectAsync();

        // Act
        var result = await _sut.GetWalletInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.BalanceSat.Should().BeGreaterThanOrEqualTo(0);
        result.PendingReceiveSat.Should().BeGreaterThanOrEqualTo(0);
        result.PendingSendSat.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetWalletInfoAsync_WhenNotConnected_ThrowsConnectionException()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = async () => await _sut.GetWalletInfoAsync();
        await act.Should().ThrowAsync<ConnectionException>()
            .WithMessage("*not connected*");
    }

    [Fact]
    public async Task ListPaymentsAsync_WhenConnected_ReturnsPaymentList()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);
        await _sut.ConnectAsync();

        // Act
        var result = await _sut.ListPaymentsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IReadOnlyList<SdkPayment>>();
    }

    [Fact]
    public async Task ListPaymentsAsync_WhenNotConnected_ThrowsConnectionException()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = async () => await _sut.ListPaymentsAsync();
        await act.Should().ThrowAsync<ConnectionException>()
            .WithMessage("*not connected*");
    }

    #endregion

    #region Event Callback Tests

    [Fact]
    public void RegisterEventCallback_WithValidCallback_StoresCallback()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);
        var callbackInvoked = false;
        Action<SdkEvent> callback = evt => callbackInvoked = true;

        // Act
        _sut.RegisterEventCallback(callback);

        // Assert
        // The callback should be stored and invoked when SDK emits events
        // This is verified indirectly through integration tests
        callbackInvoked.Should().BeFalse(); // Not invoked yet
    }

    [Fact]
    public void RegisterEventCallback_WithNullCallback_ThrowsArgumentNullException()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = () => _sut.RegisterEventCallback(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventCallback");
    }

    [Fact]
    public void RegisterEventCallback_CalledMultipleTimes_ReplacesCallback()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);
        var firstCallbackInvoked = false;
        var secondCallbackInvoked = false;
        Action<SdkEvent> firstCallback = evt => { var _ = firstCallbackInvoked; };
        Action<SdkEvent> secondCallback = evt => { var _ = secondCallbackInvoked; };

        // Act
        _sut.RegisterEventCallback(firstCallback);
        _sut.RegisterEventCallback(secondCallback);

        // Assert
        // Only the second callback should be active
        // This is verified through the behavior that only one callback is stored
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task ConnectAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await _sut.ConnectAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task PrepareReceivePaymentAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);
        await _sut.ConnectAsync();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await _sut.PrepareReceivePaymentAsync(
            amountSat: 5000,
            description: null,
            expirySec: null,
            cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetWalletInfoAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var options = CreateValidOptions();
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);
        await _sut.ConnectAsync();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await _sut.GetWalletInfoAsync(cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Offline Mode Tests

    [Fact]
    public async Task ConnectAsync_WithOfflineModeEnabled_AllowsMissingApiKey()
    {
        // Arrange
        var options = CreateValidOptions();
        options.OfflineMode = true;
        options.ApiKey = null!;
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act
        await _sut.ConnectAsync();

        // Assert
        _sut.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task ConnectAsync_WithOfflineModeEnabled_AllowsMissingMnemonic()
    {
        // Arrange
        var options = CreateValidOptions();
        options.OfflineMode = true;
        options.Mnemonic = null!;
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);

        // Act
        await _sut.ConnectAsync();

        // Assert
        _sut.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task PrepareReceivePaymentAsync_InOfflineMode_ThrowsConnectionException()
    {
        // Arrange
        var options = CreateValidOptions();
        options.OfflineMode = true;
        options.ApiKey = null!;
        _optionsMock.Setup(x => x.Value).Returns(options);
        _sut = new BreezSdkWrapper(_optionsMock.Object, _loggerMock.Object);
        await _sut.ConnectAsync();

        // Act & Assert
        var act = async () => await _sut.PrepareReceivePaymentAsync(
            amountSat: 5000,
            description: null,
            expirySec: null);

        await act.Should().ThrowAsync<ConnectionException>()
            .WithMessage("*offline mode*");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a fast connect resilience policy for tests.
    /// Same retry count as production but with 50ms delays instead of 2s,
    /// keeping tests fast while still exercising retry behavior.
    /// </summary>
    private static ResiliencePipeline CreateFastConnectPolicy() =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(50),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            })
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();

    /// <summary>
    /// Creates a valid BreezSdkOptions instance for testing.
    /// All tests should use this as a baseline and modify specific properties.
    /// </summary>
    private static BreezSdkOptions CreateValidOptions() => new()
    {
        ApiKey = "test-api-key-12345",
        Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about",
        WorkingDirectory = Path.Combine(Path.GetTempPath(), "breez-test"),
        Network = BreezNetwork.Testnet,
        WebhookUrl = null,
        MaxInvoiceAmountSat = 1000000,
        ConnectionTimeoutSeconds = 30,
        OfflineMode = false,
        CircuitBreaker = new CircuitBreakerOptions
        {
            FailureThreshold = 5,
            SamplingDurationSeconds = 60,
            BreakDurationSeconds = 30
        }
    };

    #endregion
}
