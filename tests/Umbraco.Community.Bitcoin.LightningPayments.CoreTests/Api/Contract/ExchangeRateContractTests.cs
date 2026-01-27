using System.Text.Json;
using FluentAssertions;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public.Dto;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Api.Contract;

/// <summary>
/// Contract tests for exchange rate API endpoints.
/// Verifies DTO serialization matches public-api.yaml schema.
/// </summary>
public class ExchangeRateContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    #region ExchangeRatesResponse Contract

    [Fact]
    public void ExchangeRatesResponse_SerializesAllRequiredProperties()
    {
        // Arrange
        var response = new ExchangeRatesResponse
        {
            Rates = new Dictionary<string, decimal>
            {
                ["USD"] = 45000.00m,
                ["EUR"] = 41500.00m,
                ["GBP"] = 35000.00m
            },
            FetchedAt = new DateTimeOffset(2026, 1, 27, 12, 0, 0, TimeSpan.Zero),
            Source = "CoinGecko",
            IsStale = false
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);

        // Assert
        json.Should().Contain("\"rates\"");
        json.Should().Contain("\"fetchedAt\"");
        json.Should().Contain("\"source\"");
        json.Should().Contain("\"isStale\"");
        json.Should().Contain("CoinGecko");
    }

    [Fact]
    public void ExchangeRatesResponse_DeserializesFromApiResponse()
    {
        // Arrange - simulate API response JSON
        var json = """
        {
            "rates": {"USD": 45000.00, "EUR": 41500.00},
            "fetchedAt": "2026-01-27T12:00:00+00:00",
            "source": "CoinGecko",
            "isStale": false
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<ExchangeRatesResponse>(json, JsonOptions);

        // Assert
        response.Should().NotBeNull();
        response!.Rates.Should().HaveCount(2);
        response.Rates["USD"].Should().Be(45000.00m);
        response.Source.Should().Be("CoinGecko");
        response.IsStale.Should().BeFalse();
    }

    [Fact]
    public void ExchangeRatesResponse_RatesProperty_IsObjectWithDecimalValues()
    {
        // Arrange
        var response = new ExchangeRatesResponse
        {
            Rates = new Dictionary<string, decimal>
            {
                ["USD"] = 45000.12345678m
            },
            FetchedAt = DateTimeOffset.UtcNow,
            Source = "CoinGecko"
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ExchangeRatesResponse>(json, JsonOptions);

        // Assert - round-trip preserves decimal precision
        deserialized!.Rates["USD"].Should().Be(45000.12345678m);
    }

    #endregion

    #region ConversionResponse Contract

    [Fact]
    public void ConversionResponse_SerializesAllRequiredProperties()
    {
        // Arrange
        var response = new ConversionResponse
        {
            Sats = 1_000_000,
            Currency = "USD",
            FiatAmount = 450.00m,
            FormattedAmount = "$450.00",
            RatePerBtc = 45000.00m,
            IsStale = false
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);

        // Assert
        json.Should().Contain("\"sats\"");
        json.Should().Contain("\"currency\"");
        json.Should().Contain("\"fiatAmount\"");
        json.Should().Contain("\"formattedAmount\"");
        json.Should().Contain("\"ratePerBtc\"");
        json.Should().Contain("\"isStale\"");
    }

    [Fact]
    public void ConversionResponse_DeserializesFromApiResponse()
    {
        // Arrange
        var json = """
        {
            "sats": 1000000,
            "currency": "USD",
            "fiatAmount": 450.00,
            "formattedAmount": "$450.00",
            "ratePerBtc": 45000.00,
            "isStale": false
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<ConversionResponse>(json, JsonOptions);

        // Assert
        response.Should().NotBeNull();
        response!.Sats.Should().Be(1_000_000);
        response.Currency.Should().Be("USD");
        response.FiatAmount.Should().Be(450.00m);
        response.FormattedAmount.Should().Be("$450.00");
        response.RatePerBtc.Should().Be(45000.00m);
        response.IsStale.Should().BeFalse();
    }

    [Fact]
    public void ConversionResponse_FormattedAmount_IsOptional()
    {
        // Arrange
        var response = new ConversionResponse
        {
            Sats = 1000,
            Currency = "XYZ",
            FiatAmount = 0.50m,
            FormattedAmount = null,
            RatePerBtc = 50000m
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ConversionResponse>(json, JsonOptions);

        // Assert
        deserialized!.FormattedAmount.Should().BeNull();
    }

    #endregion
}
