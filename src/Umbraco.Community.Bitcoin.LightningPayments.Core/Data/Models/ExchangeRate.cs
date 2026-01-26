using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

/// <summary>
/// Cached exchange rate for a fiat currency.
/// </summary>
public class ExchangeRate
{
    /// <summary>
    /// ISO 4217 currency code (USD, EUR, GBP, etc.).
    /// </summary>
    [Key]
    [MaxLength(3)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Exchange rate per 1 BTC (e.g., 45000.00 for USD).
    /// </summary>
    public decimal RatePerBtc { get; set; }

    /// <summary>
    /// Exchange rate per 1 satoshi (RatePerBtc / 100_000_000).
    /// </summary>
    [NotMapped]
    public decimal RatePerSat => RatePerBtc / 100_000_000m;

    /// <summary>
    /// When this rate was fetched from the source.
    /// </summary>
    public DateTimeOffset FetchedAt { get; set; }

    /// <summary>
    /// Source of the exchange rate (e.g., "CoinGecko").
    /// </summary>
    [MaxLength(50)]
    public string Source { get; set; } = "CoinGecko";

    /// <summary>
    /// Whether this rate is considered stale (>5 minutes old).
    /// </summary>
    [NotMapped]
    public bool IsStale => DateTimeOffset.UtcNow - FetchedAt > TimeSpan.FromMinutes(5);
}
