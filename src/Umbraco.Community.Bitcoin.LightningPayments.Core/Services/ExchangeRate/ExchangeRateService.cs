using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.ExchangeRate;

/// <summary>
/// Application service for exchange rate operations with in-memory caching and graceful degradation.
/// Falls back to stale cached rates when the external API is unavailable.
/// </summary>
public class ExchangeRateService : IExchangeRateService
{
    private readonly ICoinGeckoClient _coinGeckoClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExchangeRateService> _logger;
    private readonly ExchangeRateOptions _options;

    private const string CacheKeyPrefix = "exchange_rates:";
    private const string StaleCacheKeyPrefix = "exchange_rates_stale:";

    public ExchangeRateService(
        ICoinGeckoClient coinGeckoClient,
        IMemoryCache cache,
        ILogger<ExchangeRateService> logger,
        IOptions<ExchangeRateOptions> options)
    {
        _coinGeckoClient = coinGeckoClient ?? throw new ArgumentNullException(nameof(coinGeckoClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, decimal>> GetExchangeRatesAsync(
        string[] currencies,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(currencies);
        if (currencies.Length == 0)
            throw new ArgumentException("At least one currency is required.", nameof(currencies));

        var normalizedCurrencies = currencies.Select(c => c.ToUpperInvariant()).Distinct().ToArray();
        var cacheKey = CacheKeyPrefix + string.Join(",", normalizedCurrencies.OrderBy(c => c));

        // Try fresh cache first
        if (_cache.TryGetValue(cacheKey, out Dictionary<string, decimal>? cachedRates) && cachedRates != null)
        {
            _logger.LogDebug("Exchange rates served from cache for {Currencies}", string.Join(",", normalizedCurrencies));
            return cachedRates;
        }

        try
        {
            // Fetch from API
            var rates = await _coinGeckoClient.GetBitcoinPricesAsync(normalizedCurrencies, ct);

            // Cache with configured TTL
            var cacheTtl = TimeSpan.FromMinutes(_options.Cache.CacheDurationMinutes);
            _cache.Set(cacheKey, rates, cacheTtl);

            // Also store as stale fallback with longer TTL
            var staleTtl = TimeSpan.FromMinutes(_options.Cache.StaleCacheDurationMinutes);
            _cache.Set(StaleCacheKeyPrefix + cacheKey, rates, staleTtl);

            _logger.LogDebug("Fetched and cached {Count} exchange rates", rates.Count);
            return rates;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to fetch exchange rates, attempting stale cache fallback");

            // Try stale cache fallback
            var staleCacheKey = StaleCacheKeyPrefix + cacheKey;
            if (_cache.TryGetValue(staleCacheKey, out Dictionary<string, decimal>? staleRates) && staleRates != null)
            {
                _logger.LogInformation("Returning stale cached rates for graceful degradation");
                return staleRates;
            }

            _logger.LogError(ex, "Exchange rate service unavailable and no cached rates available");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<decimal?> ConvertSatoshiToFiatAsync(
        long sats,
        string currency,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        if (sats == 0)
            return 0m;

        try
        {
            var rates = await GetExchangeRatesAsync([currency.ToUpperInvariant()], ct);

            if (rates.TryGetValue(currency.ToUpperInvariant(), out var ratePerBtc))
            {
                // 1 BTC = 100,000,000 satoshis
                var ratePerSat = ratePerBtc / 100_000_000m;
                return Math.Round(sats * ratePerSat, 2);
            }

            _logger.LogWarning("No rate available for currency {Currency}", currency);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert {Sats} sats to {Currency}", sats, currency);
            return null;
        }
    }
}
