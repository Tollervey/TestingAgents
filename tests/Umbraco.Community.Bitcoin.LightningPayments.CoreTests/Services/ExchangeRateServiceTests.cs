using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.ExchangeRate;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services;

/// <summary>
/// Unit tests for ExchangeRateService.
/// Tests verify exchange rate fetching, caching, and graceful degradation.
/// </summary>
public class ExchangeRateServiceTests : IDisposable
{
    private readonly Mock<ICoinGeckoClient> _coinGeckoClientMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<ExchangeRateService>> _loggerMock;
    private readonly IOptions<ExchangeRateOptions> _options;
    private readonly ExchangeRateService _sut;

    public ExchangeRateServiceTests()
    {
        _coinGeckoClientMock = new Mock<ICoinGeckoClient>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<ExchangeRateService>>();
        _options = Options.Create(new ExchangeRateOptions
        {
            Enabled = true,
            SupportedCurrencies = ["USD", "EUR", "GBP"],
            DefaultCurrency = "USD",
            Cache = new ExchangeRateCacheOptions
            {
                CacheDurationMinutes = 5,
                StaleCacheDurationMinutes = 60
            }
        });

        _sut = new ExchangeRateService(
            _coinGeckoClientMock.Object,
            _cache,
            _loggerMock.Object,
            _options);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    #region GetExchangeRatesAsync Tests

    [Fact]
    public async Task GetExchangeRatesAsync_WithValidCurrencies_ReturnsRates()
    {
        // Arrange
        var expected = new Dictionary<string, decimal>
        {
            ["USD"] = 45000.50m,
            ["EUR"] = 41200.75m
        };

        _coinGeckoClientMock
            .Setup(c => c.GetBitcoinPricesAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GetExchangeRatesAsync(["USD", "EUR"]);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result["USD"].Should().Be(45000.50m);
        result["EUR"].Should().Be(41200.75m);
    }

    [Fact]
    public async Task GetExchangeRatesAsync_WithCachedRates_ReturnsCachedWithoutApiCall()
    {
        // Arrange
        var rates = new Dictionary<string, decimal> { ["USD"] = 45000m };
        _coinGeckoClientMock
            .Setup(c => c.GetBitcoinPricesAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates);

        // First call populates cache
        await _sut.GetExchangeRatesAsync(["USD"]);

        // Reset mock to track second call
        _coinGeckoClientMock.Invocations.Clear();

        // Act - second call should use cache
        var result = await _sut.GetExchangeRatesAsync(["USD"]);

        // Assert
        result.Should().NotBeNull();
        result["USD"].Should().Be(45000m);
        _coinGeckoClientMock.Verify(
            c => c.GetBitcoinPricesAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetExchangeRatesAsync_WhenApiUnavailable_ReturnsStaleCachedRates()
    {
        // Arrange - First call succeeds and populates both fresh and stale cache
        var rates = new Dictionary<string, decimal> { ["USD"] = 44000m };
        _coinGeckoClientMock
            .SetupSequence(c => c.GetBitcoinPricesAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates)
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        await _sut.GetExchangeRatesAsync(["USD"]);

        // Expire the fresh cache by using a new key pattern (different currencies forces fresh fetch)
        // Instead, we'll directly remove the fresh cache entry
        // The stale cache should still have the rates
        _cache.Remove("exchange_rates:USD");

        // Act
        var result = await _sut.GetExchangeRatesAsync(["USD"]);

        // Assert
        result.Should().NotBeNull();
        result["USD"].Should().Be(44000m);
    }

    [Fact]
    public async Task GetExchangeRatesAsync_WhenApiUnavailableAndNoCache_ThrowsException()
    {
        // Arrange
        _coinGeckoClientMock
            .Setup(c => c.GetBitcoinPricesAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        // Act & Assert
        await FluentActions.Invoking(() => _sut.GetExchangeRatesAsync(["USD"]))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetExchangeRatesAsync_WithEmptyCurrencies_ThrowsArgumentException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _sut.GetExchangeRatesAsync([]))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetExchangeRatesAsync_WithNullCurrencies_ThrowsArgumentNullException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _sut.GetExchangeRatesAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region ConvertSatoshiToFiatAsync Tests

    [Fact]
    public async Task ConvertSatoshiToFiatAsync_WithValidInputs_ReturnsCorrectAmount()
    {
        // Arrange - 1 BTC = $45000, so 1000 sats = $0.00045 = $0.00
        // More useful: 100,000 sats at $45000/BTC = $0.045 = $0.05 rounded
        var rates = new Dictionary<string, decimal> { ["USD"] = 45000m };
        _coinGeckoClientMock
            .Setup(c => c.GetBitcoinPricesAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates);

        // Act - 1,000,000 sats (0.01 BTC) at $45000/BTC = $450
        var result = await _sut.ConvertSatoshiToFiatAsync(1_000_000, "USD");

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(450.00m);
    }

    [Fact]
    public async Task ConvertSatoshiToFiatAsync_WhenRateUnavailable_ReturnsNull()
    {
        // Arrange
        _coinGeckoClientMock
            .Setup(c => c.GetBitcoinPricesAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        // Act
        var result = await _sut.ConvertSatoshiToFiatAsync(1000, "USD");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ConvertSatoshiToFiatAsync_WithZeroSats_ReturnsZero()
    {
        // Act
        var result = await _sut.ConvertSatoshiToFiatAsync(0, "USD");

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public async Task ConvertSatoshiToFiatAsync_WithSmallAmount_ReturnsRoundedValue()
    {
        // Arrange - 1000 sats at $45000/BTC = 1000 * (45000/100,000,000) = $0.45
        var rates = new Dictionary<string, decimal> { ["USD"] = 45000m };
        _coinGeckoClientMock
            .Setup(c => c.GetBitcoinPricesAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates);

        // Act
        var result = await _sut.ConvertSatoshiToFiatAsync(1000, "USD");

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(0.45m);
    }

    [Fact]
    public async Task ConvertSatoshiToFiatAsync_WithEmptyCurrency_ThrowsArgumentException()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _sut.ConvertSatoshiToFiatAsync(1000, ""))
            .Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
