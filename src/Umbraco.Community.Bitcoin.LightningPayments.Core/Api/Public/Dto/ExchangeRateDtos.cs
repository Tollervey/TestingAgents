using System.Text.Json.Serialization;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public.Dto;

/// <summary>
/// Response containing Bitcoin exchange rates for configured fiat currencies.
/// Matches ExchangeRatesResponse schema in public-api.yaml.
/// </summary>
public class ExchangeRatesResponse
{
    /// <summary>
    /// Exchange rates keyed by ISO 4217 currency code (e.g., {"USD": 45000.00, "EUR": 41500.00}).
    /// </summary>
    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Rates { get; set; } = new();

    /// <summary>
    /// When these rates were fetched from the source.
    /// </summary>
    [JsonPropertyName("fetchedAt")]
    public DateTimeOffset FetchedAt { get; set; }

    /// <summary>
    /// Data source for the rates (e.g., "CoinGecko").
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = "CoinGecko";

    /// <summary>
    /// True if rates are older than 5 minutes.
    /// </summary>
    [JsonPropertyName("isStale")]
    public bool IsStale { get; set; }
}

/// <summary>
/// Response containing a satoshi-to-fiat conversion result.
/// Matches ConversionResponse schema in public-api.yaml.
/// </summary>
public class ConversionResponse
{
    /// <summary>
    /// Input amount in satoshis.
    /// </summary>
    [JsonPropertyName("sats")]
    public long Sats { get; set; }

    /// <summary>
    /// Target fiat currency code.
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Converted fiat amount.
    /// </summary>
    [JsonPropertyName("fiatAmount")]
    public decimal FiatAmount { get; set; }

    /// <summary>
    /// Locale-formatted amount string (e.g., "$0.45").
    /// </summary>
    [JsonPropertyName("formattedAmount")]
    public string? FormattedAmount { get; set; }

    /// <summary>
    /// Rate used for 1 BTC in the target currency.
    /// </summary>
    [JsonPropertyName("ratePerBtc")]
    public decimal RatePerBtc { get; set; }

    /// <summary>
    /// Whether the rate used is stale (>5 minutes old).
    /// </summary>
    [JsonPropertyName("isStale")]
    public bool IsStale { get; set; }
}
