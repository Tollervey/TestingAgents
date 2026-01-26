using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Dashboard;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Api;

/// <summary>
/// Unit tests for DashboardController.
/// Tests follow TDD RED-GREEN-REFACTOR pattern.
/// These tests are written FIRST and will FAIL until the implementation is created.
/// </summary>
public class DashboardControllerTests
{
    private readonly Mock<IDashboardStatsService> _dashboardStatsServiceMock;
    private readonly DashboardController _sut;

    public DashboardControllerTests()
    {
        _dashboardStatsServiceMock = new Mock<IDashboardStatsService>();
        _sut = new DashboardController(_dashboardStatsServiceMock.Object);
    }

    #region GetDashboardStats Tests

    [Fact]
    public async Task GetDashboardStats_WithValidRequest_ReturnsOkWithStats()
    {
        // Arrange
        var expectedStats = new DashboardStats
        {
            TotalReceivedSat = 1_000_000,
            PendingCount = 5,
            DailyVolumeSat = 50_000,
            WalletBalanceSat = 250_000,
            SdkConnected = true,
            LastPaymentAt = DateTimeOffset.UtcNow.AddHours(-2)
        };

        _dashboardStatsServiceMock
            .Setup(s => s.GetDashboardStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _sut.GetDashboardStats(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();

        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as DashboardStatsResponse;
        response.Should().NotBeNull();
        response!.TotalReceivedSat.Should().Be(expectedStats.TotalReceivedSat);
        response.PendingCount.Should().Be(expectedStats.PendingCount);
        response.DailyVolumeSat.Should().Be(expectedStats.DailyVolumeSat);
        response.WalletBalanceSat.Should().Be(expectedStats.WalletBalanceSat);
        response.SdkConnected.Should().Be(expectedStats.SdkConnected);
        response.LastPaymentAt.Should().Be(expectedStats.LastPaymentAt);

        _dashboardStatsServiceMock.Verify(
            s => s.GetDashboardStatsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDashboardStats_WhenServiceThrowsException_ThrowsException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Database connection failed");

        _dashboardStatsServiceMock
            .Setup(s => s.GetDashboardStatsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var act = async () => await _sut.GetDashboardStats(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }

    [Fact]
    public async Task GetDashboardStats_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var stats = new DashboardStats
        {
            TotalReceivedSat = 0,
            PendingCount = 0,
            DailyVolumeSat = 0,
            WalletBalanceSat = 0,
            SdkConnected = false
        };

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _dashboardStatsServiceMock
            .Setup(s => s.GetDashboardStatsAsync(cancellationToken))
            .ReturnsAsync(stats);

        // Act
        await _sut.GetDashboardStats(cancellationToken);

        // Assert
        _dashboardStatsServiceMock.Verify(
            s => s.GetDashboardStatsAsync(cancellationToken),
            Times.Once);
    }

    #endregion

    #region GetPaymentChart Tests

    [Fact]
    public async Task GetPaymentChart_WithDefaultPeriod_UsesWeek()
    {
        // Arrange
        var expectedChartData = new ChartData
        {
            Period = ChartPeriod.Week,
            DataPoints = new List<ChartDataPoint>
            {
                new() { Timestamp = DateTimeOffset.UtcNow.AddDays(-6), AmountSat = 10_000, Count = 2 },
                new() { Timestamp = DateTimeOffset.UtcNow.AddDays(-5), AmountSat = 15_000, Count = 3 },
                new() { Timestamp = DateTimeOffset.UtcNow, AmountSat = 20_000, Count = 5 }
            }
        };

        _dashboardStatsServiceMock
            .Setup(s => s.GetPaymentChartDataAsync(ChartPeriod.Week, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedChartData);

        // Act
        var result = await _sut.GetPaymentChart("week", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();

        var okResult = result.Result as OkObjectResult;
        var chartData = okResult!.Value as ChartDataResponse;
        chartData.Should().NotBeNull();
        chartData!.Period.Should().Be("week");
        chartData.DataPoints.Should().HaveCount(3);

        _dashboardStatsServiceMock.Verify(
            s => s.GetPaymentChartDataAsync(ChartPeriod.Week, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("day", ChartPeriod.Day)]
    [InlineData("week", ChartPeriod.Week)]
    [InlineData("month", ChartPeriod.Month)]
    public async Task GetPaymentChart_WithValidPeriod_ReturnsChartData(string periodString, ChartPeriod expectedPeriod)
    {
        // Arrange
        var expectedChartData = new ChartData
        {
            Period = expectedPeriod,
            DataPoints = new List<ChartDataPoint>
            {
                new() { Timestamp = DateTimeOffset.UtcNow, AmountSat = 100_000, Count = 10 }
            }
        };

        _dashboardStatsServiceMock
            .Setup(s => s.GetPaymentChartDataAsync(expectedPeriod, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedChartData);

        // Act
        var result = await _sut.GetPaymentChart(periodString, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();

        var okResult = result.Result as OkObjectResult;
        var chartData = okResult!.Value as ChartDataResponse;
        chartData.Should().NotBeNull();
        chartData!.Period.Should().Be(periodString);

        _dashboardStatsServiceMock.Verify(
            s => s.GetPaymentChartDataAsync(expectedPeriod, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("DAY", ChartPeriod.Day)]
    [InlineData("Week", ChartPeriod.Week)]
    [InlineData("MONTH", ChartPeriod.Month)]
    public async Task GetPaymentChart_WithCaseInsensitivePeriod_ParsesCorrectly(string periodString, ChartPeriod expectedPeriod)
    {
        // Arrange
        var expectedChartData = new ChartData
        {
            Period = expectedPeriod,
            DataPoints = Array.Empty<ChartDataPoint>()
        };

        _dashboardStatsServiceMock
            .Setup(s => s.GetPaymentChartDataAsync(expectedPeriod, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedChartData);

        // Act
        var result = await _sut.GetPaymentChart(periodString, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();

        _dashboardStatsServiceMock.Verify(
            s => s.GetPaymentChartDataAsync(expectedPeriod, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("year")]
    [InlineData("")]
    [InlineData("123")]
    public async Task GetPaymentChart_WithInvalidPeriod_ReturnsBadRequest(string invalidPeriod)
    {
        // Act
        var result = await _sut.GetPaymentChart(invalidPeriod, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<BadRequestObjectResult>();

        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();

        _dashboardStatsServiceMock.Verify(
            s => s.GetPaymentChartDataAsync(It.IsAny<ChartPeriod>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPaymentChart_WhenServiceThrowsException_ThrowsException()
    {
        // Arrange
        _dashboardStatsServiceMock
            .Setup(s => s.GetPaymentChartDataAsync(It.IsAny<ChartPeriod>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Chart generation failed"));

        // Act
        var act = async () => await _sut.GetPaymentChart("week", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Chart generation failed");
    }

    #endregion

    #region GetWalletBalance Tests

    [Fact]
    public async Task GetWalletBalance_WithValidRequest_ReturnsOkWithBalance()
    {
        // Arrange
        var expectedBalance = new WalletBalance
        {
            BalanceSat = 500_000,
            PendingReceiveSat = 10_000,
            PendingSendSat = 5_000
        };

        _dashboardStatsServiceMock
            .Setup(s => s.GetWalletBalanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBalance);

        // Act
        var result = await _sut.GetWalletBalance(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();

        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as WalletBalanceResponse;
        response.Should().NotBeNull();
        response!.BalanceSat.Should().Be(expectedBalance.BalanceSat);
        response.PendingReceiveSat.Should().Be(expectedBalance.PendingReceiveSat);
        response.PendingSendSat.Should().Be(expectedBalance.PendingSendSat);

        _dashboardStatsServiceMock.Verify(
            s => s.GetWalletBalanceAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetWalletBalance_WithZeroBalance_ReturnsOkWithZeroValues()
    {
        // Arrange
        var expectedBalance = new WalletBalance
        {
            BalanceSat = 0,
            PendingReceiveSat = 0,
            PendingSendSat = 0
        };

        _dashboardStatsServiceMock
            .Setup(s => s.GetWalletBalanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBalance);

        // Act
        var result = await _sut.GetWalletBalance(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();

        var okResult = result.Result as OkObjectResult;
        var balance = okResult!.Value as WalletBalanceResponse;
        balance.Should().NotBeNull();
        balance!.BalanceSat.Should().Be(0);
        balance.PendingReceiveSat.Should().Be(0);
        balance.PendingSendSat.Should().Be(0);
    }

    [Fact]
    public async Task GetWalletBalance_WhenServiceThrowsException_ThrowsException()
    {
        // Arrange
        _dashboardStatsServiceMock
            .Setup(s => s.GetWalletBalanceAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Wallet connection failed"));

        // Act
        var act = async () => await _sut.GetWalletBalance(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Wallet connection failed");
    }

    [Fact]
    public async Task GetWalletBalance_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var balance = new WalletBalance
        {
            BalanceSat = 100_000,
            PendingReceiveSat = 0,
            PendingSendSat = 0
        };

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _dashboardStatsServiceMock
            .Setup(s => s.GetWalletBalanceAsync(cancellationToken))
            .ReturnsAsync(balance);

        // Act
        await _sut.GetWalletBalance(cancellationToken);

        // Assert
        _dashboardStatsServiceMock.Verify(
            s => s.GetWalletBalanceAsync(cancellationToken),
            Times.Once);
    }

    #endregion

    #region GetWalletLimits Tests

    [Fact]
    public async Task GetWalletLimits_WithValidRequest_ReturnsOkWithLimits()
    {
        // Arrange
        var expectedLimits = new WalletLimits
        {
            Receive = new LimitRange { MinSat = 1_000, MaxSat = 1_000_000 },
            Send = new LimitRange { MinSat = 500, MaxSat = 500_000 }
        };

        _dashboardStatsServiceMock
            .Setup(s => s.GetWalletLimitsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLimits);

        // Act
        var result = await _sut.GetWalletLimits(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();

        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as WalletLimitsResponse;
        response.Should().NotBeNull();
        response!.Receive.MinSat.Should().Be(expectedLimits.Receive.MinSat);
        response.Receive.MaxSat.Should().Be(expectedLimits.Receive.MaxSat);
        response.Send.MinSat.Should().Be(expectedLimits.Send.MinSat);
        response.Send.MaxSat.Should().Be(expectedLimits.Send.MaxSat);

        _dashboardStatsServiceMock.Verify(
            s => s.GetWalletLimitsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetWalletLimits_WithTypicalLightningLimits_ReturnsCorrectRanges()
    {
        // Arrange
        var expectedLimits = new WalletLimits
        {
            Receive = new LimitRange { MinSat = 1_000, MaxSat = 4_294_967 },
            Send = new LimitRange { MinSat = 1_000, MaxSat = 4_294_967 }
        };

        _dashboardStatsServiceMock
            .Setup(s => s.GetWalletLimitsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLimits);

        // Act
        var result = await _sut.GetWalletLimits(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();

        var okResult = result.Result as OkObjectResult;
        var limits = okResult!.Value as WalletLimitsResponse;
        limits.Should().NotBeNull();
        limits!.Receive.MinSat.Should().Be(1_000);
        limits.Receive.MaxSat.Should().Be(4_294_967);
        limits.Send.MinSat.Should().Be(1_000);
        limits.Send.MaxSat.Should().Be(4_294_967);
    }

    [Fact]
    public async Task GetWalletLimits_WhenServiceThrowsException_ThrowsException()
    {
        // Arrange
        _dashboardStatsServiceMock
            .Setup(s => s.GetWalletLimitsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SDK not available"));

        // Act
        var act = async () => await _sut.GetWalletLimits(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SDK not available");
    }

    [Fact]
    public async Task GetWalletLimits_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var limits = new WalletLimits
        {
            Receive = new LimitRange { MinSat = 1_000, MaxSat = 1_000_000 },
            Send = new LimitRange { MinSat = 1_000, MaxSat = 1_000_000 }
        };

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _dashboardStatsServiceMock
            .Setup(s => s.GetWalletLimitsAsync(cancellationToken))
            .ReturnsAsync(limits);

        // Act
        await _sut.GetWalletLimits(cancellationToken);

        // Assert
        _dashboardStatsServiceMock.Verify(
            s => s.GetWalletLimitsAsync(cancellationToken),
            Times.Once);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullDashboardStatsService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new DashboardController(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dashboardStatsService");
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var controller = new DashboardController(_dashboardStatsServiceMock.Object);

        // Assert
        controller.Should().NotBeNull();
    }

    #endregion
}
