using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.Core.Exceptions;
using Breez.Sdk.Liquid.Extensions.Core.Infrastructure;
using Breez.Sdk.Liquid.Extensions.TestUtilities.Fakes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Services;

/// <summary>
/// Unit tests for BreezSdkService invoice creation and core operations.
/// These tests verify the service layer's business logic WITHOUT requiring the real BreezSDK.
/// </summary>
/// <remarks>
/// TDD RED PHASE: These tests reference BreezSdkService which doesn't exist yet.
/// They should fail compilation initially, then pass once the implementation is complete.
/// </remarks>
public class BreezSdkServiceTests
{
    private readonly FakeBreezSdkWrapper _fakeWrapper;
    private readonly BreezSdkOptions _options;
    private readonly NullLogger<BreezSdkService> _logger;
    private BreezSdkService _sut;

    public BreezSdkServiceTests()
    {
        _fakeWrapper = new FakeBreezSdkWrapper();
        _options = CreateValidOptions();
        _logger = new NullLogger<BreezSdkService>();

        // System under test - will be created per test
        _sut = null!;
    }

    #region Invoice Creation Tests

    [Fact]
    public async Task CreateInvoiceAsync_WithValidAmount_ReturnsSuccessWithInvoice()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);
        await _sut.ConnectAsync();
        const ulong amountSat = 5000;
        const string description = "Test payment";

        // Act
        var result = await _sut.CreateInvoiceAsync(amountSat, description);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AmountSat.Should().Be(amountSat);
        result.Value.Description.Should().Be(description);
        result.Value.Destination.Should().StartWith("lnbc");
        result.Value.PaymentHash.Should().NotBeNullOrEmpty();
        result.Value.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithZeroAmount_ReturnsFailure()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);
        await _sut.ConnectAsync();
        const ulong amountSat = 0;

        // Act
        var result = await _sut.CreateInvoiceAsync(amountSat);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(BreezErrorCode.AmountBelowMinimum);
        result.Error.Message.Should().Contain("greater than zero");
    }

    [Fact]
    public async Task CreateInvoiceAsync_WhenAmountExceedsMaximum_ReturnsFailure()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);
        await _sut.ConnectAsync();
        ulong amountSat = _options.MaxInvoiceAmountSat + 1;

        // Act
        var result = await _sut.CreateInvoiceAsync(amountSat);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(BreezErrorCode.AmountAboveMaximum);
        result.Error.Message.Should().Contain("exceeds maximum");
        result.Error.Message.Should().Contain(_options.MaxInvoiceAmountSat.ToString());
    }

    [Fact]
    public async Task CreateInvoiceAsync_WhenDescriptionExceedsMaxLength_ReturnsFailure()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);
        await _sut.ConnectAsync();
        const ulong amountSat = 5000;
        var longDescription = new string('x', _options.MaxInvoiceDescriptionLength + 1);

        // Act
        var result = await _sut.CreateInvoiceAsync(amountSat, longDescription);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(BreezErrorCode.InvalidInvoice);
        result.Error.Message.Should().Contain("description");
        result.Error.Message.Should().Contain("too long");
    }

    [Fact]
    public async Task CreateInvoiceAsync_WhenNotConnected_ReturnsFailure()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);
        // NOTE: Not calling ConnectAsync
        const ulong amountSat = 5000;

        // Act
        var result = await _sut.CreateInvoiceAsync(amountSat);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(BreezErrorCode.SdkNotConnected);
        result.Error.Message.Should().Contain("not connected");
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithoutExpiry_UsesDefaultExpiry()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);
        await _sut.ConnectAsync();
        const ulong amountSat = 5000;

        // Act
        var result = await _sut.CreateInvoiceAsync(amountSat);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        // Default expiry is typically 3600 seconds (1 hour)
        var expectedExpiry = DateTimeOffset.UtcNow.AddSeconds(3600);
        result.Value!.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithCustomExpiry_UsesProvidedExpiry()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);
        await _sut.ConnectAsync();
        const ulong amountSat = 5000;
        const uint expirySec = 7200; // 2 hours

        // Act
        var result = await _sut.CreateInvoiceAsync(amountSat, expirySec: expirySec);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var expectedExpiry = DateTimeOffset.UtcNow.AddSeconds(expirySec);
        result.Value!.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateInvoiceAsync_MapsResponseCorrectly()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);
        await _sut.ConnectAsync();
        const ulong amountSat = 10000;
        const string description = "Detailed payment description";

        // Act
        var result = await _sut.CreateInvoiceAsync(amountSat, description);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        var invoice = result.Value!;
        invoice.PaymentHash.Should().NotBeNullOrEmpty();
        invoice.Destination.Should().NotBeNullOrEmpty();
        invoice.Destination.Should().StartWith("lnbc"); // Bitcoin mainnet prefix
        invoice.AmountSat.Should().Be(amountSat);
        invoice.Description.Should().Be(description);
        invoice.Type.Should().Be(InvoiceType.Bolt11);
        invoice.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        invoice.ExpiresAt.Should().BeAfter(invoice.CreatedAt);
        invoice.FeeSat.Should().Be(0); // FakeWrapper returns 0 fees
    }

    [Fact]
    public async Task CreateInvoiceAsync_WhenSdkThrowsException_ReturnsFailure()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);
        await _sut.ConnectAsync();

        var sdkException = new PaymentException(
            BreezErrorCode.InvoiceCreationFailed,
            "SDK internal error");
        _fakeWrapper.PrepareReceivePaymentException = sdkException;

        // Act
        var result = await _sut.CreateInvoiceAsync(5000);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(BreezErrorCode.InvoiceCreationFailed);
        result.Error.Exception.Should().Be(sdkException);
    }

    #endregion

    #region Connection Tests

    [Fact]
    public async Task ConnectAsync_CallsWrapperConnect()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);
        _fakeWrapper.IsConnected.Should().BeFalse();

        // Act
        await _sut.ConnectAsync();

        // Assert
        _fakeWrapper.IsConnected.Should().BeTrue();
        _sut.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task ConnectAsync_WhenOptionsInvalid_ThrowsConfigurationException()
    {
        // Arrange
        var invalidOptions = new BreezSdkOptions
        {
            ApiKey = "", // Invalid - empty API key
            Mnemonic = "test mnemonic",
            MaxInvoiceAmountSat = 10_000_000,
            MaxInvoiceDescriptionLength = 200
        };
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(invalidOptions), _logger);

        // Act & Assert
        await Assert.ThrowsAsync<ConfigurationException>(() => _sut.ConnectAsync());
    }

    [Fact]
    public async Task ConnectAsync_WhenMnemonicMissing_ThrowsConfigurationException()
    {
        // Arrange
        var invalidOptions = new BreezSdkOptions
        {
            ApiKey = "test-api-key",
            Mnemonic = "", // Invalid - empty mnemonic
            MaxInvoiceAmountSat = 10_000_000,
            MaxInvoiceDescriptionLength = 200
        };
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(invalidOptions), _logger);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConfigurationException>(() => _sut.ConnectAsync());
        exception.ErrorCode.Should().Be(BreezErrorCode.MnemonicMissing);
    }

    [Fact]
    public async Task DisconnectAsync_CallsWrapperDisconnect()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);
        await _sut.ConnectAsync();
        _fakeWrapper.IsConnected.Should().BeTrue();

        // Act
        await _sut.DisconnectAsync();

        // Assert
        _fakeWrapper.IsConnected.Should().BeFalse();
        _sut.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task DisconnectAsync_WhenNotConnected_DoesNotThrow()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);

        // Act
        var act = async () => await _sut.DisconnectAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task IsConnected_ReturnsWrapperConnectionState()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);

        // Act & Assert - Initially not connected
        _sut.IsConnected.Should().BeFalse();

        // Simulate connection
        await _fakeWrapper.ConnectAsync();
        _sut.IsConnected.Should().BeTrue();
    }

    #endregion

    #region Balance Tests

    [Fact]
    public async Task GetBalanceAsync_WhenConnected_ReturnsSuccessWithBalance()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);
        await _sut.ConnectAsync();
        const ulong expectedBalance = 100_000;
        _fakeWrapper.Balance = expectedBalance;

        // Act
        var result = await _sut.GetBalanceAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedBalance);
    }

    [Fact]
    public async Task GetBalanceAsync_WhenNotConnected_ReturnsFailure()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);
        // NOTE: Not calling ConnectAsync

        // Act
        var result = await _sut.GetBalanceAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(BreezErrorCode.SdkNotConnected);
    }

    [Fact]
    public async Task GetBalanceAsync_WhenBalanceIsZero_ReturnsSuccessWithZero()
    {
        // Arrange
        _sut = new BreezSdkService(_fakeWrapper, Options.Create(_options), _logger);
        await _sut.ConnectAsync();
        _fakeWrapper.Balance = 0;

        // Act
        var result = await _sut.GetBalanceAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    private static BreezSdkOptions CreateValidOptions() => new()
    {
        ApiKey = "test-api-key",
        Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about",
        MaxInvoiceAmountSat = 10_000_000,
        MaxInvoiceDescriptionLength = 200
    };

    #endregion
}
