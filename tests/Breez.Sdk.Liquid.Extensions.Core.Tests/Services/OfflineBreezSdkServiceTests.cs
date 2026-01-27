using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.Core.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Services;

/// <summary>
/// Unit tests for OfflineBreezSdkService.
/// These tests verify the offline/mock implementation behavior for development scenarios.
/// </summary>
public class OfflineBreezSdkServiceTests
{
    private readonly Mock<IOptions<BreezSdkOptions>> _optionsMock;
    private readonly Mock<IPaymentRepository> _repositoryMock;
    private OfflineBreezSdkService? _sut;

    public OfflineBreezSdkServiceTests()
    {
        _optionsMock = new Mock<IOptions<BreezSdkOptions>>();
        _repositoryMock = new Mock<IPaymentRepository>();

        SetupDefaultOptions();
    }

    #region Connection Tests

    [Fact]
    public async Task ConnectAsync_SucceedsImmediately()
    {
        // Arrange
        _sut = CreateService();

        // Act
        await _sut.ConnectAsync();

        // Assert
        _sut.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task IsConnected_ReturnsTrueAfterConnect()
    {
        // Arrange
        _sut = CreateService();

        // Act
        await _sut.ConnectAsync();

        // Assert
        _sut.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task DisconnectAsync_SucceedsImmediately()
    {
        // Arrange
        _sut = CreateService();
        await _sut.ConnectAsync();

        // Act
        await _sut.DisconnectAsync();

        // Assert - should complete without exception
        _sut.IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task IsConnectedAsync_AlwaysReturnsTrueAfterConnect()
    {
        // Arrange
        _sut = CreateService();
        await _sut.ConnectAsync();

        // Act
        var result = await _sut.IsConnectedAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsConnectedAsync_ReturnsFalseBeforeConnect()
    {
        // Arrange
        _sut = CreateService();

        // Act
        var result = await _sut.IsConnectedAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Invoice Creation Tests

    [Fact]
    public async Task CreateInvoiceAsync_ReturnsSuccessWithSyntheticInvoice()
    {
        // Arrange
        _sut = CreateService();
        await _sut.ConnectAsync();
        const ulong amountSat = 5000;
        const string description = "Test invoice";

        // Act
        var result = await _sut.CreateInvoiceAsync(amountSat, description);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AmountSat.Should().Be(amountSat);
        result.Value.Description.Should().Be(description);
        result.Value.Destination.Should().StartWith("lnbc"); // BOLT11 format
        result.Value.PaymentHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateInvoiceAsync_GeneratesUniquePaymentHashes()
    {
        // Arrange
        _sut = CreateService();
        await _sut.ConnectAsync();

        // Act
        var invoice1 = await _sut.CreateInvoiceAsync(1000);
        var invoice2 = await _sut.CreateInvoiceAsync(1000);

        // Assert
        invoice1.IsSuccess.Should().BeTrue();
        invoice2.IsSuccess.Should().BeTrue();
        invoice1.Value!.PaymentHash.Should().NotBe(invoice2.Value!.PaymentHash);
    }

    [Fact]
    public async Task CreateInvoiceAsync_RespectsProvidedAmount()
    {
        // Arrange
        _sut = CreateService();
        await _sut.ConnectAsync();
        const ulong expectedAmount = 25000;

        // Act
        var result = await _sut.CreateInvoiceAsync(expectedAmount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AmountSat.Should().Be(expectedAmount);
    }

    [Fact]
    public async Task CreateInvoiceAsync_RespectsProvidedDescription()
    {
        // Arrange
        _sut = CreateService();
        await _sut.ConnectAsync();
        const string expectedDescription = "Coffee payment";

        // Act
        var result = await _sut.CreateInvoiceAsync(1000, expectedDescription);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Description.Should().Be(expectedDescription);
    }

    [Fact]
    public async Task CreateInvoiceAsync_UsesDefaultExpiryWhenNotSpecified()
    {
        // Arrange
        _sut = CreateService();
        await _sut.ConnectAsync();

        // Act
        var result = await _sut.CreateInvoiceAsync(1000);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var expectedExpiry = DateTimeOffset.UtcNow.AddHours(1);
        result.Value!.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateInvoiceAsync_UsesCustomExpiryWhenSpecified()
    {
        // Arrange
        _sut = CreateService();
        await _sut.ConnectAsync();
        const uint customExpirySec = 7200; // 2 hours

        // Act
        var result = await _sut.CreateInvoiceAsync(1000, expirySec: customExpirySec);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var expectedExpiry = DateTimeOffset.UtcNow.AddSeconds(customExpirySec);
        result.Value!.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateInvoiceAsync_AppliesSimulatedDelayWhenConfigured()
    {
        // Arrange
        SetupOptionsWithDelay(delayMilliseconds: 100);
        _sut = CreateService();
        await _sut.ConnectAsync();

        // Act
        var startTime = DateTimeOffset.UtcNow;
        var result = await _sut.CreateInvoiceAsync(1000);
        var elapsed = DateTimeOffset.UtcNow - startTime;

        // Assert
        result.IsSuccess.Should().BeTrue();
        elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(90)); // Allow small variance
    }

    [Fact]
    public async Task CreateInvoiceAsync_PersistsPaymentState()
    {
        // Arrange
        _sut = CreateService();
        await _sut.ConnectAsync();
        PaymentState? capturedPayment = null;
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PaymentState>(), It.IsAny<CancellationToken>()))
            .Callback<PaymentState, CancellationToken>((p, _) => capturedPayment = p)
            .ReturnsAsync((PaymentState p, CancellationToken _) => p);

        // Act
        var result = await _sut.CreateInvoiceAsync(5000, "Test payment");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<PaymentState>(), It.IsAny<CancellationToken>()), Times.Once);
        capturedPayment.Should().NotBeNull();
        capturedPayment!.PaymentHash.Should().Be(result.Value!.PaymentHash);
        capturedPayment.AmountSat.Should().Be(5000);
        capturedPayment.Description.Should().Be("Test payment");
        capturedPayment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public async Task CreateInvoiceAsync_FailsWhenNotConnected()
    {
        // Arrange
        _sut = CreateService();
        // Note: Not calling ConnectAsync

        // Act
        var result = await _sut.CreateInvoiceAsync(1000);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(BreezErrorCode.ConnectionError);
    }

    #endregion

    #region Balance Tests

    [Fact]
    public async Task GetBalanceAsync_ReturnsConfiguredMockBalance()
    {
        // Arrange
        const ulong expectedBalance = 150000;
        SetupOptionsWithBalance(expectedBalance);
        _sut = CreateService();
        await _sut.ConnectAsync();

        // Act
        var result = await _sut.GetBalanceAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedBalance);
    }

    [Fact]
    public async Task GetBalanceAsync_ReturnsDefaultBalanceWhenNotConfigured()
    {
        // Arrange
        _sut = CreateService();
        await _sut.ConnectAsync();

        // Act
        var result = await _sut.GetBalanceAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
        result.Value.Should().Be(100000); // Default mock balance
    }

    [Fact]
    public async Task GetBalanceAsync_FailsWhenNotConnected()
    {
        // Arrange
        _sut = CreateService();

        // Act
        var result = await _sut.GetBalanceAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(BreezErrorCode.ConnectionError);
    }

    #endregion

    #region Payment History Tests

    [Fact]
    public async Task GetPaymentByHashAsync_ReturnsPaymentIfInvoiceWasCreated()
    {
        // Arrange
        _sut = CreateService();
        await _sut.ConnectAsync();
        var invoice = await _sut.CreateInvoiceAsync(5000, "Test");
        var paymentHash = invoice.Value!.PaymentHash;

        _repositoryMock
            .Setup(x => x.GetByHashAsync(paymentHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentState
            {
                PaymentHash = paymentHash,
                AmountSat = 5000,
                Status = PaymentStatus.Pending
            });

        // Act
        var result = await _sut.GetPaymentByHashAsync(paymentHash);

        // Assert
        result.Should().NotBeNull();
        result!.PaymentHash.Should().Be(paymentHash);
        result.AmountSat.Should().Be(5000);
    }

    [Fact]
    public async Task GetPaymentByHashAsync_ReturnsNullForUnknownHash()
    {
        // Arrange
        _sut = CreateService();
        await _sut.ConnectAsync();
        const string unknownHash = "abc123def456";

        _repositoryMock
            .Setup(x => x.GetByHashAsync(unknownHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentState?)null);

        // Act
        var result = await _sut.GetPaymentByHashAsync(unknownHash);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPaymentHistoryAsync_ReturnsEmptyListInitially()
    {
        // Arrange
        _sut = CreateService();
        await _sut.ConnectAsync();

        _repositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentState>());

        // Act
        var result = await _sut.GetPaymentHistoryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPaymentHistoryAsync_ReturnsCreatedInvoicesAsPendingPayments()
    {
        // Arrange
        _sut = CreateService();
        await _sut.ConnectAsync();

        var invoice1 = await _sut.CreateInvoiceAsync(1000, "Payment 1");
        var invoice2 = await _sut.CreateInvoiceAsync(2000, "Payment 2");

        var payments = new List<PaymentState>
        {
            new()
            {
                PaymentHash = invoice1.Value!.PaymentHash,
                AmountSat = 1000,
                Description = "Payment 1",
                Status = PaymentStatus.Pending
            },
            new()
            {
                PaymentHash = invoice2.Value!.PaymentHash,
                AmountSat = 2000,
                Description = "Payment 2",
                Status = PaymentStatus.Pending
            }
        };

        _repositoryMock
            .Setup(x => x.GetAllAsync(0, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        // Act
        var result = await _sut.GetPaymentHistoryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.PaymentHash == invoice1.Value!.PaymentHash);
        result.Should().Contain(p => p.PaymentHash == invoice2.Value!.PaymentHash);
    }

    [Fact]
    public async Task GetPaymentHistoryAsync_RespectsPaginationParameters()
    {
        // Arrange
        _sut = CreateService();
        await _sut.ConnectAsync();
        const int offset = 10;
        const int limit = 25;

        _repositoryMock
            .Setup(x => x.GetAllAsync(offset, limit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PaymentState>());

        // Act
        await _sut.GetPaymentHistoryAsync(offset, limit);

        // Assert
        _repositoryMock.Verify(x => x.GetAllAsync(offset, limit, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task CreateInvoiceAsync_RespectsSimulatePaymentDelayOption()
    {
        // Arrange
        SetupOptionsWithDelay(delayMilliseconds: 200);
        _sut = CreateService();
        await _sut.ConnectAsync();

        // Act
        var startTime = DateTimeOffset.UtcNow;
        var result = await _sut.CreateInvoiceAsync(1000);
        var elapsed = DateTimeOffset.UtcNow - startTime;

        // Assert
        result.IsSuccess.Should().BeTrue();
        elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(180));
    }

    [Fact]
    public async Task CreateInvoiceAsync_NoDelayWhenSimulatePaymentDelayIsZero()
    {
        // Arrange
        SetupOptionsWithDelay(delayMilliseconds: 0);
        _sut = CreateService();
        await _sut.ConnectAsync();

        // Act
        var startTime = DateTimeOffset.UtcNow;
        var result = await _sut.CreateInvoiceAsync(1000);
        var elapsed = DateTimeOffset.UtcNow - startTime;

        // Assert
        result.IsSuccess.Should().BeTrue();
        elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(100)); // Should be very fast
    }

    [Fact]
    public async Task CreateInvoiceAsync_NeverFailsWhenSimulateFailureRateIsZero()
    {
        // Arrange
        SetupOptionsWithFailureRate(failureRate: 0.0);
        _sut = CreateService();
        await _sut.ConnectAsync();

        // Act - Create 10 invoices
        var results = new List<OperationResult<Invoice>>();
        for (int i = 0; i < 10; i++)
        {
            results.Add(await _sut.CreateInvoiceAsync(1000));
        }

        // Assert - All should succeed
        results.Should().OnlyContain(r => r.IsSuccess);
    }

    [Fact]
    public async Task CreateInvoiceAsync_AlwaysFailsWhenSimulateFailureRateIsOne()
    {
        // Arrange
        SetupOptionsWithFailureRate(failureRate: 1.0);
        _sut = CreateService();
        await _sut.ConnectAsync();

        // Act - Create 5 invoices
        var results = new List<OperationResult<Invoice>>();
        for (int i = 0; i < 5; i++)
        {
            results.Add(await _sut.CreateInvoiceAsync(1000));
        }

        // Assert - All should fail
        results.Should().OnlyContain(r => r.IsFailure);
        results.Should().OnlyContain(r => r.Error != null && r.Error.Code == BreezErrorCode.PaymentFailed);
    }

    [Fact]
    public async Task CreateInvoiceAsync_PartiallyFailsWhenSimulateFailureRateIsPartial()
    {
        // Arrange
        SetupOptionsWithFailureRate(failureRate: 0.5); // 50% failure rate
        _sut = CreateService();
        await _sut.ConnectAsync();

        // Act - Create 100 invoices for better statistical significance
        var results = new List<OperationResult<Invoice>>();
        for (int i = 0; i < 100; i++)
        {
            results.Add(await _sut.CreateInvoiceAsync(1000));
        }

        // Assert - Should have mix of successes and failures
        var successCount = results.Count(r => r.IsSuccess);
        var failureCount = results.Count(r => r.IsFailure);

        successCount.Should().BeGreaterThan(0);
        failureCount.Should().BeGreaterThan(0);
        // With 100 samples and 50% rate, we expect roughly 50 of each (allow reasonable variance)
        successCount.Should().BeInRange(35, 65);
    }

    #endregion

    #region Helper Methods

    private void SetupDefaultOptions()
    {
        _optionsMock.Setup(x => x.Value).Returns(new BreezSdkOptions
        {
            OfflineMode = true,
            MaxInvoiceAmountSat = 10_000_000,
            MaxInvoiceDescriptionLength = 200,
            OfflineSimulateDelayMs = 0,
            OfflineSimulateFailureRate = 0.0,
            OfflineMockBalanceSat = 100000
        });
    }

    private void SetupOptionsWithDelay(int delayMilliseconds)
    {
        _optionsMock.Setup(x => x.Value).Returns(new BreezSdkOptions
        {
            OfflineMode = true,
            MaxInvoiceAmountSat = 10_000_000,
            MaxInvoiceDescriptionLength = 200,
            OfflineSimulateDelayMs = delayMilliseconds,
            OfflineSimulateFailureRate = 0.0,
            OfflineMockBalanceSat = 100000
        });
    }

    private void SetupOptionsWithBalance(ulong balance)
    {
        _optionsMock.Setup(x => x.Value).Returns(new BreezSdkOptions
        {
            OfflineMode = true,
            MaxInvoiceAmountSat = 10_000_000,
            MaxInvoiceDescriptionLength = 200,
            OfflineSimulateDelayMs = 0,
            OfflineSimulateFailureRate = 0.0,
            OfflineMockBalanceSat = balance
        });
    }

    private void SetupOptionsWithFailureRate(double failureRate)
    {
        _optionsMock.Setup(x => x.Value).Returns(new BreezSdkOptions
        {
            OfflineMode = true,
            MaxInvoiceAmountSat = 10_000_000,
            MaxInvoiceDescriptionLength = 200,
            OfflineSimulateDelayMs = 0,
            OfflineSimulateFailureRate = failureRate,
            OfflineMockBalanceSat = 100000
        });
    }

    private OfflineBreezSdkService CreateService() => new(
        _optionsMock.Object,
        _repositoryMock.Object,
        NullLogger<OfflineBreezSdkService>.Instance
    );

    #endregion
}
