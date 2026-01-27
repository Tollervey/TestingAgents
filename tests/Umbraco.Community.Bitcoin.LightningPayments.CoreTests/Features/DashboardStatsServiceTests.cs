using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Dashboard;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Features;

/// <summary>
/// Unit tests for DashboardStatsService.
/// Tests verify behavior defined by management-api.yaml contract.
/// </summary>
public class DashboardStatsServiceTests : IDisposable
{
    private readonly PaymentDbContext _context;
    private readonly Mock<IBreezSdkService> _breezSdkServiceMock;
    private readonly Mock<IBreezSdkHandleProvider> _handleProviderMock;
    private readonly Mock<IBreezSdkWrapper> _wrapperMock;
    private readonly Mock<ILogger<DashboardStatsService>> _loggerMock;
    private readonly IDashboardStatsService _sut;

    public DashboardStatsServiceTests()
    {
        // Arrange: Create in-memory database with unique name per test instance
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"DashboardStatsTest_{Guid.NewGuid()}")
            .Options;
        _context = new PaymentDbContext(options);
        _context.Database.EnsureCreated();

        // Mock dependencies
        _breezSdkServiceMock = new Mock<IBreezSdkService>();
        _handleProviderMock = new Mock<IBreezSdkHandleProvider>();
        _wrapperMock = new Mock<IBreezSdkWrapper>();
        _loggerMock = new Mock<ILogger<DashboardStatsService>>();

        // System Under Test
        _sut = new DashboardStatsService(
            _context,
            _breezSdkServiceMock.Object,
            _handleProviderMock.Object,
            _wrapperMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetDashboardStatsAsync Tests

    [Fact]
    public async Task GetDashboardStatsAsync_WithNoPayments_ReturnsZeroStats()
    {
        // Arrange
        _breezSdkServiceMock
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Simulate wallet balance from Breez SDK
        var mockPayments = new List<global::Breez.Sdk.Liquid.Payment>();
        _breezSdkServiceMock
            .Setup(s => s.GetPaymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPayments);

        // Act
        var result = await _sut.GetDashboardStatsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalReceivedSat.Should().Be(0);
        result.PendingCount.Should().Be(0);
        result.DailyVolumeSat.Should().Be(0);
        result.WalletBalanceSat.Should().Be(0);
        result.SdkConnected.Should().BeTrue();
        result.LastPaymentAt.Should().BeNull();
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WithPaidPayments_ReturnsSumOfPaidAmounts()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await SeedPayment("hash1", PaymentStatus.Paid, 1000, now.AddHours(-2));
        await SeedPayment("hash2", PaymentStatus.Paid, 2000, now.AddHours(-1));
        await SeedPayment("hash3", PaymentStatus.Pending, 500, now.AddMinutes(-30)); // Should not count
        await SeedPayment("hash4", PaymentStatus.Failed, 300, now.AddMinutes(-15)); // Should not count

        _breezSdkServiceMock
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockPayments = new List<global::Breez.Sdk.Liquid.Payment>();
        _breezSdkServiceMock
            .Setup(s => s.GetPaymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPayments);

        // Act
        var result = await _sut.GetDashboardStatsAsync();

        // Assert
        result.TotalReceivedSat.Should().Be(3000); // Only paid payments
        result.PendingCount.Should().Be(1);
        result.SdkConnected.Should().BeTrue();
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WithPendingPayments_ReturnsCorrectPendingCount()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await SeedPayment("hash1", PaymentStatus.Pending, 1000, now.AddMinutes(-10));
        await SeedPayment("hash2", PaymentStatus.Pending, 2000, now.AddMinutes(-5));
        await SeedPayment("hash3", PaymentStatus.Paid, 500, now.AddMinutes(-1));

        _breezSdkServiceMock
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockPayments = new List<global::Breez.Sdk.Liquid.Payment>();
        _breezSdkServiceMock
            .Setup(s => s.GetPaymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPayments);

        // Act
        var result = await _sut.GetDashboardStatsAsync();

        // Assert
        result.PendingCount.Should().Be(2);
        result.TotalReceivedSat.Should().Be(500); // Only paid
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WithPaymentsInLast24Hours_ReturnsCorrectDailyVolume()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await SeedPayment("hash1", PaymentStatus.Paid, 1000, now.AddHours(-23)); // Within 24h
        await SeedPayment("hash2", PaymentStatus.Paid, 2000, now.AddHours(-12)); // Within 24h
        await SeedPayment("hash3", PaymentStatus.Paid, 500, now.AddHours(-1)); // Within 24h
        await SeedPayment("hash4", PaymentStatus.Paid, 3000, now.AddHours(-25)); // Outside 24h

        _breezSdkServiceMock
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockPayments = new List<global::Breez.Sdk.Liquid.Payment>();
        _breezSdkServiceMock
            .Setup(s => s.GetPaymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPayments);

        // Act
        var result = await _sut.GetDashboardStatsAsync();

        // Assert
        result.DailyVolumeSat.Should().Be(3500); // Sum of payments within 24h
        result.TotalReceivedSat.Should().Be(6500); // All paid payments
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WithSdkDisconnected_ReturnsFalseForConnection()
    {
        // Arrange
        _breezSdkServiceMock
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var mockPayments = new List<global::Breez.Sdk.Liquid.Payment>();
        _breezSdkServiceMock
            .Setup(s => s.GetPaymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPayments);

        // Act
        var result = await _sut.GetDashboardStatsAsync();

        // Assert
        result.SdkConnected.Should().BeFalse();
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WithWalletBalance_ReturnsBalanceFromSdk()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await SeedPayment("hash1", PaymentStatus.Paid, 5000, now.AddHours(-1));

        _breezSdkServiceMock
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Mock SDK returning wallet info with balance
        var mockPayment = new global::Breez.Sdk.Liquid.Payment.Receive(
            destination: "addr",
            txId: "txid",
            timestamp: (ulong)now.ToUnixTimeSeconds(),
            amountSat: 12500
        );
        var mockPayments = new List<global::Breez.Sdk.Liquid.Payment> { mockPayment };
        _breezSdkServiceMock
            .Setup(s => s.GetPaymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPayments);

        // Act
        var result = await _sut.GetDashboardStatsAsync();

        // Assert
        result.WalletBalanceSat.Should().Be(12500);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WithMultiplePayments_ReturnsLatestPaymentTimestamp()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var latest = now.AddMinutes(-5);
        await SeedPayment("hash1", PaymentStatus.Paid, 1000, now.AddHours(-2));
        await SeedPayment("hash2", PaymentStatus.Paid, 2000, latest); // Latest
        await SeedPayment("hash3", PaymentStatus.Paid, 500, now.AddHours(-1));

        _breezSdkServiceMock
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockPayments = new List<global::Breez.Sdk.Liquid.Payment>();
        _breezSdkServiceMock
            .Setup(s => s.GetPaymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPayments);

        // Act
        var result = await _sut.GetDashboardStatsAsync();

        // Assert
        result.LastPaymentAt.Should().NotBeNull();
        result.LastPaymentAt.Should().BeCloseTo(latest, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetDashboardStatsAsync_OnlyCountsPaidPaymentsForLastPaymentAt()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await SeedPayment("hash1", PaymentStatus.Paid, 1000, now.AddHours(-2));
        await SeedPayment("hash2", PaymentStatus.Pending, 2000, now.AddMinutes(-1)); // More recent but pending

        _breezSdkServiceMock
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockPayments = new List<global::Breez.Sdk.Liquid.Payment>();
        _breezSdkServiceMock
            .Setup(s => s.GetPaymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPayments);

        // Act
        var result = await _sut.GetDashboardStatsAsync();

        // Assert
        result.LastPaymentAt.Should().NotBeNull();
        result.LastPaymentAt.Should().BeCloseTo(now.AddHours(-2), TimeSpan.FromSeconds(1));
    }

    #endregion

    #region GetPaymentChartDataAsync Tests

    [Fact]
    public async Task GetPaymentChartDataAsync_DayPeriod_Returns24DataPoints()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await SeedPayment("hash1", PaymentStatus.Paid, 1000, now.AddHours(-5));
        await SeedPayment("hash2", PaymentStatus.Paid, 2000, now.AddHours(-5).AddMinutes(-30));
        await SeedPayment("hash3", PaymentStatus.Paid, 1500, now.AddHours(-10));

        _breezSdkServiceMock
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockPayments = new List<global::Breez.Sdk.Liquid.Payment>();
        _breezSdkServiceMock
            .Setup(s => s.GetPaymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPayments);

        // Act
        var result = await _sut.GetPaymentChartDataAsync(ChartPeriod.Day);

        // Assert
        result.Should().NotBeNull();
        result.Period.Should().Be(ChartPeriod.Day);
        result.DataPoints.Should().HaveCount(24); // One per hour
        result.DataPoints.Should().AllSatisfy(dp =>
        {
            dp.Timestamp.Should().NotBeNull();
            dp.AmountSat.Should().BeGreaterOrEqualTo(0);
            dp.Count.Should().BeGreaterOrEqualTo(0);
        });
    }

    [Fact]
    public async Task GetPaymentChartDataAsync_WeekPeriod_Returns7DataPoints()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await SeedPayment("hash1", PaymentStatus.Paid, 1000, now.AddDays(-1));
        await SeedPayment("hash2", PaymentStatus.Paid, 2000, now.AddDays(-3));
        await SeedPayment("hash3", PaymentStatus.Paid, 1500, now.AddDays(-5));

        _breezSdkServiceMock
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockPayments = new List<global::Breez.Sdk.Liquid.Payment>();
        _breezSdkServiceMock
            .Setup(s => s.GetPaymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPayments);

        // Act
        var result = await _sut.GetPaymentChartDataAsync(ChartPeriod.Week);

        // Assert
        result.Should().NotBeNull();
        result.Period.Should().Be(ChartPeriod.Week);
        result.DataPoints.Should().HaveCount(7); // One per day
    }

    [Fact]
    public async Task GetPaymentChartDataAsync_MonthPeriod_Returns30DataPoints()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await SeedPayment("hash1", PaymentStatus.Paid, 1000, now.AddDays(-5));
        await SeedPayment("hash2", PaymentStatus.Paid, 2000, now.AddDays(-15));
        await SeedPayment("hash3", PaymentStatus.Paid, 1500, now.AddDays(-25));

        _breezSdkServiceMock
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockPayments = new List<global::Breez.Sdk.Liquid.Payment>();
        _breezSdkServiceMock
            .Setup(s => s.GetPaymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPayments);

        // Act
        var result = await _sut.GetPaymentChartDataAsync(ChartPeriod.Month);

        // Assert
        result.Should().NotBeNull();
        result.Period.Should().Be(ChartPeriod.Month);
        result.DataPoints.Should().HaveCount(30); // One per day
    }

    [Fact]
    public async Task GetPaymentChartDataAsync_GroupsPaymentsByTimeSlot()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var targetHour = now.AddHours(-5);

        // Multiple payments in the same hour
        await SeedPayment("hash1", PaymentStatus.Paid, 1000, targetHour);
        await SeedPayment("hash2", PaymentStatus.Paid, 2000, targetHour.AddMinutes(15));
        await SeedPayment("hash3", PaymentStatus.Paid, 500, targetHour.AddMinutes(45));

        _breezSdkServiceMock
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Mock SDK payments with timestamps
        var mockPayments = new List<global::Breez.Sdk.Liquid.Payment>
        {
            CreateMockPayment(1000, targetHour),
            CreateMockPayment(2000, targetHour.AddMinutes(15)),
            CreateMockPayment(500, targetHour.AddMinutes(45))
        };
        _breezSdkServiceMock
            .Setup(s => s.GetPaymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPayments);

        // Act
        var result = await _sut.GetPaymentChartDataAsync(ChartPeriod.Day);

        // Assert
        var targetDataPoint = result.DataPoints
            .FirstOrDefault(dp => Math.Abs((dp.Timestamp - targetHour).TotalHours) < 1);

        targetDataPoint.Should().NotBeNull();
        targetDataPoint!.AmountSat.Should().Be(3500); // Sum of all payments in that hour
        targetDataPoint.Count.Should().Be(3);
    }

    [Fact]
    public async Task GetPaymentChartDataAsync_OnlyIncludesPaidPayments()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await SeedPayment("hash1", PaymentStatus.Paid, 1000, now.AddHours(-5));
        await SeedPayment("hash2", PaymentStatus.Pending, 2000, now.AddHours(-5).AddMinutes(15));
        await SeedPayment("hash3", PaymentStatus.Failed, 500, now.AddHours(-5).AddMinutes(30));

        _breezSdkServiceMock
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Only include the paid payment in SDK mock
        var mockPayments = new List<global::Breez.Sdk.Liquid.Payment>
        {
            CreateMockPayment(1000, now.AddHours(-5))
        };
        _breezSdkServiceMock
            .Setup(s => s.GetPaymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPayments);

        // Act
        var result = await _sut.GetPaymentChartDataAsync(ChartPeriod.Day);

        // Assert
        var targetDataPoint = result.DataPoints
            .FirstOrDefault(dp => Math.Abs((dp.Timestamp - now.AddHours(-5)).TotalHours) < 1);

        targetDataPoint.Should().NotBeNull();
        targetDataPoint!.AmountSat.Should().Be(1000); // Only paid payment
        targetDataPoint.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetPaymentChartDataAsync_WithNoPayments_ReturnsZeroDataPoints()
    {
        // Arrange - No payments seeded
        _breezSdkServiceMock
            .Setup(s => s.IsConnectedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockPayments = new List<global::Breez.Sdk.Liquid.Payment>();
        _breezSdkServiceMock
            .Setup(s => s.GetPaymentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPayments);

        // Act
        var result = await _sut.GetPaymentChartDataAsync(ChartPeriod.Day);

        // Assert
        result.Should().NotBeNull();
        result.DataPoints.Should().AllSatisfy(dp =>
        {
            dp.AmountSat.Should().Be(0);
            dp.Count.Should().Be(0);
        });
    }

    #endregion

    #region GetWalletBalanceAsync Tests

    [Fact]
    public async Task GetWalletBalanceAsync_ReturnsBalanceFromSdk()
    {
        // Arrange
        var mockSdk = new Mock<global::Breez.Sdk.Liquid.BindingLiquidSdk>();
        var mockGetInfoResult = new global::Breez.Sdk.Liquid.GetInfoResponse(
            balanceSat: 50000,
            pendingSendSat: 1000,
            pendingReceiveSat: 2000,
            pubkey: "test-pubkey"
        );

        _handleProviderMock
            .Setup(h => h.GetSdkAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSdk.Object);

        mockSdk
            .Setup(s => s.GetInfo())
            .Returns(mockGetInfoResult);

        // Act
        var result = await _sut.GetWalletBalanceAsync();

        // Assert
        result.Should().NotBeNull();
        result.BalanceSat.Should().Be(50000);
        result.PendingReceiveSat.Should().Be(2000);
        result.PendingSendSat.Should().Be(1000);
    }

    [Fact]
    public async Task GetWalletBalanceAsync_WithNoSdk_ReturnsZero()
    {
        // Arrange
        _handleProviderMock
            .Setup(h => h.GetSdkAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((global::Breez.Sdk.Liquid.BindingLiquidSdk?)null);

        // Act
        var result = await _sut.GetWalletBalanceAsync();

        // Assert
        result.Should().NotBeNull();
        result.BalanceSat.Should().Be(0);
        result.PendingReceiveSat.Should().Be(0);
        result.PendingSendSat.Should().Be(0);
    }

    [Fact]
    public async Task GetWalletBalanceAsync_WhenSdkThrows_ReturnsZero()
    {
        // Arrange
        var mockSdk = new Mock<global::Breez.Sdk.Liquid.BindingLiquidSdk>();

        _handleProviderMock
            .Setup(h => h.GetSdkAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSdk.Object);

        mockSdk
            .Setup(s => s.GetInfo())
            .Throws(new Exception("SDK error"));

        // Act
        var result = await _sut.GetWalletBalanceAsync();

        // Assert
        result.Should().NotBeNull();
        result.BalanceSat.Should().Be(0);
        result.PendingReceiveSat.Should().Be(0);
        result.PendingSendSat.Should().Be(0);
    }

    #endregion

    #region GetWalletLimitsAsync Tests

    [Fact]
    public async Task GetWalletLimitsAsync_ReturnsLimitsFromSdk()
    {
        // Arrange
        var mockSdk = new Mock<global::Breez.Sdk.Liquid.BindingLiquidSdk>();
        var mockLimits = new global::Breez.Sdk.Liquid.LightningPaymentLimitsResponse(
            send: new global::Breez.Sdk.Liquid.Limits(minSat: 1000, maxSat: 5000000, maxZeroConfSat: 1000000),
            receive: new global::Breez.Sdk.Liquid.Limits(minSat: 100, maxSat: 10000000, maxZeroConfSat: 2000000)
        );

        _handleProviderMock
            .Setup(h => h.GetSdkAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSdk.Object);

        _wrapperMock
            .Setup(w => w.FetchLightningLimitsAsync(mockSdk.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockLimits);

        // Act
        var result = await _sut.GetWalletLimitsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Receive.Should().NotBeNull();
        result.Receive.MinSat.Should().Be(100);
        result.Receive.MaxSat.Should().Be(10000000);
        result.Send.Should().NotBeNull();
        result.Send.MinSat.Should().Be(1000);
        result.Send.MaxSat.Should().Be(5000000);
    }

    [Fact]
    public async Task GetWalletLimitsAsync_WithNoSdk_ReturnsZeroLimits()
    {
        // Arrange
        _handleProviderMock
            .Setup(h => h.GetSdkAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((global::Breez.Sdk.Liquid.BindingLiquidSdk?)null);

        // Act
        var result = await _sut.GetWalletLimitsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Receive.MinSat.Should().Be(0);
        result.Receive.MaxSat.Should().Be(0);
        result.Send.MinSat.Should().Be(0);
        result.Send.MaxSat.Should().Be(0);
    }

    [Fact]
    public async Task GetWalletLimitsAsync_WhenSdkThrows_ReturnsZeroLimits()
    {
        // Arrange
        var mockSdk = new Mock<global::Breez.Sdk.Liquid.BindingLiquidSdk>();

        _handleProviderMock
            .Setup(h => h.GetSdkAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSdk.Object);

        _wrapperMock
            .Setup(w => w.FetchLightningLimitsAsync(mockSdk.Object, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SDK error"));

        // Act
        var result = await _sut.GetWalletLimitsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Receive.MinSat.Should().Be(0);
        result.Receive.MaxSat.Should().Be(0);
        result.Send.MinSat.Should().Be(0);
        result.Send.MaxSat.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Seeds a payment into the in-memory database for testing.
    /// Note: PaymentState doesn't have timestamp - the implementation gets timing data from BreezSDK.
    /// </summary>
    private async Task SeedPayment(string paymentHash, PaymentStatus status, ulong amountSat, DateTimeOffset timestamp)
    {
        var payment = new PaymentState
        {
            PaymentHash = paymentHash,
            Status = status,
            AmountSat = amountSat,
            ContentId = 1,
            UserSessionId = $"session-{Guid.NewGuid()}",
            Kind = PaymentKind.Paywall
        };

        _context.PaymentStates.Add(payment);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a mock Breez SDK payment for testing.
    /// </summary>
    private static global::Breez.Sdk.Liquid.Payment CreateMockPayment(ulong amountSat, DateTimeOffset timestamp)
    {
        return new global::Breez.Sdk.Liquid.Payment.Receive(
            destination: $"addr-{Guid.NewGuid():N}",
            txId: $"tx-{Guid.NewGuid():N}",
            timestamp: (ulong)timestamp.ToUnixTimeSeconds(),
            amountSat: amountSat
        );
    }

    #endregion
}
