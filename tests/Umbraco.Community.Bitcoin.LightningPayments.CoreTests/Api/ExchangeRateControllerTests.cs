using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public.Dto;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.ExchangeRate;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Api;

/// <summary>
/// Unit tests for ExchangeRateController.
/// Tests verify endpoint behavior, validation, and error handling.
/// </summary>
public class ExchangeRateControllerTests
{
    private readonly Mock<IExchangeRateService> _exchangeRateServiceMock;
    private readonly Mock<ILogger<ExchangeRateController>> _loggerMock;
    private readonly IOptions<ExchangeRateOptions> _options;
    private readonly ExchangeRateController _sut;

    public ExchangeRateControllerTests()
    {
        _exchangeRateServiceMock = new Mock<IExchangeRateService>();
        _loggerMock = new Mock<ILogger<ExchangeRateController>>();
        _options = Options.Create(new ExchangeRateOptions
        {
            SupportedCurrencies = ["USD", "EUR", "GBP"],
            DefaultCurrency = "USD"
        });

        _sut = new ExchangeRateController(
            _exchangeRateServiceMock.Object,
            _loggerMock.Object,
            _options);
    }

    #region GetExchangeRates Tests

    [Fact]
    public async Task GetExchangeRates_WithDefaultCurrencies_ReturnsOkWithRates()
    {
        // Arrange
        var rates = new Dictionary<string, decimal>
        {
            ["USD"] = 45000m,
            ["EUR"] = 41500m,
            ["GBP"] = 35000m
        };
        _exchangeRateServiceMock
            .Setup(s => s.GetExchangeRatesAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates);

        // Act
        var result = await _sut.GetExchangeRates(currencies: null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ExchangeRatesResponse>().Subject;
        response.Rates.Should().HaveCount(3);
        response.Rates["USD"].Should().Be(45000m);
        response.Source.Should().Be("CoinGecko");
    }

    [Fact]
    public async Task GetExchangeRates_WithSpecificCurrencies_PassesCurrenciesToService()
    {
        // Arrange
        string[]? capturedCurrencies = null;
        _exchangeRateServiceMock
            .Setup(s => s.GetExchangeRatesAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .Callback<string[], CancellationToken>((c, _) => capturedCurrencies = c)
            .ReturnsAsync(new Dictionary<string, decimal> { ["JPY"] = 6800000m });

        // Act
        await _sut.GetExchangeRates(currencies: "JPY");

        // Assert
        capturedCurrencies.Should().NotBeNull();
        capturedCurrencies.Should().Contain("JPY");
    }

    [Fact]
    public async Task GetExchangeRates_WhenServiceUnavailable_Returns503()
    {
        // Arrange
        _exchangeRateServiceMock
            .Setup(s => s.GetExchangeRatesAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        // Act
        var result = await _sut.GetExchangeRates();

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(503);
    }

    #endregion

    #region ConvertAmount Tests

    [Fact]
    public async Task ConvertAmount_WithValidInput_ReturnsConversion()
    {
        // Arrange
        var rates = new Dictionary<string, decimal> { ["USD"] = 45000m };
        _exchangeRateServiceMock
            .Setup(s => s.GetExchangeRatesAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates);

        // Act - 1,000,000 sats = 0.01 BTC = $450 at $45000/BTC
        var result = await _sut.ConvertAmount(sats: 1_000_000, currency: "USD");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ConversionResponse>().Subject;
        response.Sats.Should().Be(1_000_000);
        response.Currency.Should().Be("USD");
        response.FiatAmount.Should().Be(450.00m);
        response.RatePerBtc.Should().Be(45000m);
        response.FormattedAmount.Should().Be("$450.00");
    }

    [Fact]
    public async Task ConvertAmount_WithInvalidCurrency_Returns400()
    {
        // Act
        var result = await _sut.ConvertAmount(sats: 1000, currency: "");

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ConvertAmount_WithTooLongCurrency_Returns400()
    {
        // Act
        var result = await _sut.ConvertAmount(sats: 1000, currency: "ABCD");

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ConvertAmount_WhenServiceUnavailable_Returns503()
    {
        // Arrange
        _exchangeRateServiceMock
            .Setup(s => s.GetExchangeRatesAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        // Act
        var result = await _sut.ConvertAmount(sats: 1000, currency: "USD");

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task ConvertAmount_WithZeroSats_ReturnsZeroAmount()
    {
        // Arrange
        var rates = new Dictionary<string, decimal> { ["USD"] = 45000m };
        _exchangeRateServiceMock
            .Setup(s => s.GetExchangeRatesAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates);

        // Act
        var result = await _sut.ConvertAmount(sats: 0, currency: "USD");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ConversionResponse>().Subject;
        response.FiatAmount.Should().Be(0m);
    }

    [Fact]
    public async Task ConvertAmount_WithEurCurrency_FormatsWithEuroSign()
    {
        // Arrange
        var rates = new Dictionary<string, decimal> { ["EUR"] = 41500m };
        _exchangeRateServiceMock
            .Setup(s => s.GetExchangeRatesAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates);

        // Act
        var result = await _sut.ConvertAmount(sats: 100_000_000, currency: "EUR");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ConversionResponse>().Subject;
        response.FormattedAmount.Should().Be("€41500.00");
    }

    #endregion
}
