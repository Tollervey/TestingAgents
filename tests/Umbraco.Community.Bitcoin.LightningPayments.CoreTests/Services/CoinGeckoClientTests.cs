using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.ExchangeRate;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services;

/// <summary>
/// Unit tests for CoinGeckoClient.
/// Tests verify HTTP request construction, response parsing, and error handling.
/// </summary>
public class CoinGeckoClientTests
{
    private readonly Mock<ILogger<CoinGeckoClient>> _loggerMock;
    private readonly IOptions<ExchangeRateOptions> _options;

    public CoinGeckoClientTests()
    {
        _loggerMock = new Mock<ILogger<CoinGeckoClient>>();
        _options = Options.Create(new ExchangeRateOptions
        {
            CoinGecko = new CoinGeckoOptions
            {
                BaseUrl = "https://api.coingecko.com/api/v3",
                TimeoutSeconds = 10
            }
        });
    }

    private CoinGeckoClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var mockHandler = new MockHttpMessageHandler(handler);
        var httpClient = new HttpClient(mockHandler)
        {
            BaseAddress = new Uri("https://api.coingecko.com")
        };
        return new CoinGeckoClient(httpClient, _loggerMock.Object, _options);
    }

    [Fact]
    public async Task GetBitcoinPricesAsync_WithValidCurrencies_ParsesResponseCorrectly()
    {
        // Arrange
        var responseJson = """{"bitcoin":{"usd":45000.50000000,"eur":41200.75000000}}""";
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        }));

        // Act
        var result = await client.GetBitcoinPricesAsync(["USD", "EUR"]);

        // Assert
        result.Should().HaveCount(2);
        result["USD"].Should().Be(45000.50000000m);
        result["EUR"].Should().Be(41200.75000000m);
    }

    [Fact]
    public async Task GetBitcoinPricesAsync_WithSingleCurrency_ReturnsOneRate()
    {
        // Arrange
        var responseJson = """{"bitcoin":{"gbp":35500.12345678}}""";
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        }));

        // Act
        var result = await client.GetBitcoinPricesAsync(["GBP"]);

        // Assert
        result.Should().HaveCount(1);
        result["GBP"].Should().Be(35500.12345678m);
    }

    [Fact]
    public async Task GetBitcoinPricesAsync_WhenApiReturns429_ThrowsHttpRequestException()
    {
        // Arrange
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("""{"status":{"error_code":429}}""", Encoding.UTF8, "application/json")
        }));

        // Act & Assert
        await FluentActions.Invoking(() => client.GetBitcoinPricesAsync(["USD"]))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetBitcoinPricesAsync_WhenApiReturns500_ThrowsHttpRequestException()
    {
        // Arrange
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error", Encoding.UTF8, "text/plain")
        }));

        // Act & Assert
        await FluentActions.Invoking(() => client.GetBitcoinPricesAsync(["USD"]))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetBitcoinPricesAsync_WhenNetworkError_ThrowsHttpRequestException()
    {
        // Arrange
        var client = CreateClient((_, _) => throw new HttpRequestException("Network error"));

        // Act & Assert
        await FluentActions.Invoking(() => client.GetBitcoinPricesAsync(["USD"]))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetBitcoinPricesAsync_WithInvalidJson_ThrowsException()
    {
        // Arrange
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not valid json", Encoding.UTF8, "application/json")
        }));

        // Act & Assert
        await FluentActions.Invoking(() => client.GetBitcoinPricesAsync(["USD"]))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetBitcoinPricesAsync_BuildsCorrectQueryString()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var responseJson = """{"bitcoin":{"usd":45000.00000000,"eur":41000.00000000}}""";
        var client = CreateClient((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
        });

        // Act
        await client.GetBitcoinPricesAsync(["USD", "EUR"]);

        // Assert
        capturedRequest.Should().NotBeNull();
        var uri = capturedRequest!.RequestUri!.ToString();
        uri.Should().Contain("ids=bitcoin");
        uri.Should().Contain("vs_currencies=");
        uri.Should().Contain("usd");
        uri.Should().Contain("eur");
        uri.Should().Contain("precision=8");
    }

    [Fact]
    public async Task GetBitcoinPricesAsync_WithEmptyArray_ThrowsArgumentException()
    {
        // Arrange
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        // Act & Assert
        await FluentActions.Invoking(() => client.GetBitcoinPricesAsync([]))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetBitcoinPricesAsync_MissingBitcoinProperty_ThrowsException()
    {
        // Arrange
        var responseJson = """{"ethereum":{"usd":3000}}""";
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        }));

        // Act & Assert
        await FluentActions.Invoking(() => client.GetBitcoinPricesAsync(["USD"]))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*bitcoin*");
    }

    /// <summary>
    /// Helper class for mocking HttpMessageHandler in tests.
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
