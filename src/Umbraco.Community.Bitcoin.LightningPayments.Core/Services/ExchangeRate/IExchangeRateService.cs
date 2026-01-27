namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.ExchangeRate;

/// <summary>
/// Service for fetching and caching Bitcoin exchange rates.
/// Provides rate lookup and fiat conversion with graceful degradation.
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Gets Bitcoin exchange rates for the specified fiat currencies.
    /// Returns cached rates when available (5-minute TTL), fetches from API otherwise.
    /// </summary>
    /// <param name="currencies">ISO 4217 currency codes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Dictionary mapping currency code to rate per BTC.</returns>
    Task<Dictionary<string, decimal>> GetExchangeRatesAsync(
        string[] currencies,
        CancellationToken ct = default);

    /// <summary>
    /// Converts a satoshi amount to fiat currency.
    /// Returns null if the rate is unavailable (graceful degradation).
    /// </summary>
    /// <param name="sats">Amount in satoshis.</param>
    /// <param name="currency">Target fiat currency code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Fiat amount, or null if rate unavailable.</returns>
    Task<decimal?> ConvertSatoshiToFiatAsync(
        long sats,
        string currency,
        CancellationToken ct = default);
}
