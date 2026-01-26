using System.ComponentModel.DataAnnotations;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;

/// <summary>
/// Configuration options for exchange rate service (multi-currency display).
/// </summary>
public class ExchangeRateOptions
{
    /// <summary>
    /// The configuration section name in appsettings.
    /// </summary>
    public const string SectionName = "LightningPayments:ExchangeRates";

    /// <summary>
    /// Whether exchange rate fetching is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The exchange rate provider to use.
    /// </summary>
    public ExchangeRateProvider Provider { get; set; } = ExchangeRateProvider.CoinGecko;

    /// <summary>
    /// CoinGecko-specific configuration.
    /// </summary>
    public CoinGeckoOptions CoinGecko { get; set; } = new();

    /// <summary>
    /// Cache configuration for exchange rates.
    /// </summary>
    public ExchangeRateCacheOptions Cache { get; set; } = new();

    /// <summary>
    /// List of fiat currencies to fetch rates for.
    /// </summary>
    public string[] SupportedCurrencies { get; set; } = ["USD", "EUR", "GBP"];

    /// <summary>
    /// The default fiat currency to display.
    /// </summary>
    [StringLength(3, MinimumLength = 3)]
    public string DefaultCurrency { get; set; } = "USD";
}

/// <summary>
/// Exchange rate provider options.
/// </summary>
public enum ExchangeRateProvider
{
    /// <summary>
    /// CoinGecko API (free tier available).
    /// </summary>
    CoinGecko = 0,

    /// <summary>
    /// Static/manual rates (for testing or offline scenarios).
    /// </summary>
    Static = 1
}

/// <summary>
/// CoinGecko API configuration.
/// </summary>
public class CoinGeckoOptions
{
    /// <summary>
    /// The base URL for CoinGecko API.
    /// </summary>
    [Url]
    public string BaseUrl { get; set; } = "https://api.coingecko.com/api/v3";

    /// <summary>
    /// Optional API key for CoinGecko Pro (higher rate limits).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    [Range(1, 60)]
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Whether to use the Pro API endpoint (requires ApiKey).
    /// </summary>
    public bool UsePro { get; set; } = false;
}

/// <summary>
/// Cache configuration for exchange rates.
/// </summary>
public class ExchangeRateCacheOptions
{
    /// <summary>
    /// How long to cache exchange rates in minutes.
    /// Per SC-006, rates should be less than 5 minutes stale.
    /// </summary>
    [Range(1, 60)]
    public int CacheDurationMinutes { get; set; } = 5;

    /// <summary>
    /// How long to use stale cached rates if the provider is unavailable.
    /// </summary>
    [Range(1, 1440)]
    public int StaleCacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Whether to persist cached rates to the database.
    /// </summary>
    public bool PersistToDatabase { get; set; } = true;
}
