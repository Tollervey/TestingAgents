namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.ExchangeRate;

/// <summary>
/// Low-level HTTP client for CoinGecko API v3.
/// Fetches Bitcoin exchange rates for specified fiat currencies.
/// </summary>
public interface ICoinGeckoClient
{
    /// <summary>
    /// Fetches current Bitcoin prices in specified fiat currencies.
    /// </summary>
    /// <param name="fiatCurrencies">ISO 4217 currency codes (e.g., "USD", "EUR").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary mapping currency code to rate per BTC.</returns>
    Task<Dictionary<string, decimal>> GetBitcoinPricesAsync(
        string[] fiatCurrencies,
        CancellationToken ct = default);
}
