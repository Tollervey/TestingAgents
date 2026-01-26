# CoinGecko API .NET Implementation Guide

**Document ID**: CoinGecko-Implementation-001
**Date**: 2026-01-26
**Platform**: .NET 9.0, C# 12+
**Target**: FR-019 through FR-022

## Overview

This document provides production-ready .NET code for integrating CoinGecko API into the Umbraco Lightning Payments system. All examples follow constitutional principles and best practices.

---

## 1. Domain Models

### ExchangeRate Value Object

```csharp
namespace Umbraco.Community.LightningPayments.Core.Domain.ValueObjects;

/// <summary>
/// Represents a single exchange rate for a Bitcoin to fiat currency conversion.
/// Value object (immutable, no identity).
/// </summary>
public sealed record ExchangeRate
{
    /// <summary>
    /// ISO 4217 currency code (e.g., "USD", "EUR")
    /// </summary>
    public string Currency { get; init; }

    /// <summary>
    /// Rate of 1 BTC to target currency (in smallest unit)
    /// </summary>
    public decimal RatePerBitcoin { get; init; }

    /// <summary>
    /// Rate of 1 satoshi to target currency
    /// </summary>
    public decimal RatePerSatoshi => RatePerBitcoin / 100_000_000m;

    /// <summary>
    /// When this rate was fetched from the source
    /// </summary>
    public DateTimeOffset FetchedAt { get; init; }

    /// <summary>
    /// Data source (e.g., "CoinGecko", "CoinMarketCap")
    /// </summary>
    public string Source { get; init; } = "CoinGecko";

    /// <summary>
    /// Whether this rate is from cache (stale) or fresh
    /// </summary>
    public bool IsFromCache { get; init; }

    /// <summary>
    /// Creates a new ExchangeRate from satoshis and rate per BTC
    /// </summary>
    public static ExchangeRate Create(
        string currency,
        decimal ratePerBitcoin,
        DateTimeOffset fetchedAt,
        bool isFromCache = false)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency code required", nameof(currency));

        if (ratePerBitcoin <= 0)
            throw new ArgumentException("Rate must be positive", nameof(ratePerBitcoin));

        return new ExchangeRate
        {
            Currency = currency.ToUpperInvariant(),
            RatePerBitcoin = ratePerBitcoin,
            FetchedAt = fetchedAt,
            IsFromCache = isFromCache
        };
    }

    /// <summary>
    /// Converts satoshi amount to fiat currency
    /// </summary>
    public decimal ConvertFromSatoshi(long satoshis) => satoshis * RatePerSatoshi;

    /// <summary>
    /// Converts fiat amount to satoshi
    /// </summary>
    public long ConvertToSatoshi(decimal fiatAmount) =>
        (long)Math.Round(fiatAmount / RatePerSatoshi, 0);
}
```

### ExchangeRates Collection

```csharp
namespace Umbraco.Community.LightningPayments.Core.Domain.ValueObjects;

/// <summary>
/// Immutable collection of exchange rates for a given set of currencies.
/// </summary>
public sealed record ExchangeRates
{
    private readonly Dictionary<string, ExchangeRate> _rates;

    public IReadOnlyDictionary<string, ExchangeRate> Rates => _rates.AsReadOnly();

    /// <summary>
    /// When these rates were fetched
    /// </summary>
    public DateTimeOffset FetchedAt { get; init; }

    /// <summary>
    /// Whether all rates in this collection are from cache
    /// </summary>
    public bool IsFromCache { get; init; }

    public ExchangeRates(IEnumerable<ExchangeRate> rates, DateTimeOffset? fetchedAt = null)
    {
        _rates = rates
            .GroupBy(r => r.Currency)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        FetchedAt = fetchedAt ?? DateTimeOffset.UtcNow;
        IsFromCache = rates.FirstOrDefault()?.IsFromCache ?? false;
    }

    /// <summary>
    /// Gets rate for specified currency
    /// </summary>
    public Result<ExchangeRate> GetRate(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return Result.Failure<ExchangeRate>(
                new DomainError("INVALID_CURRENCY", "Currency code required"));

        if (_rates.TryGetValue(currency, out var rate))
            return Result.Success(rate);

        return Result.Failure<ExchangeRate>(
            new DomainError("CURRENCY_NOT_FOUND", $"No rate found for {currency}"));
    }

    /// <summary>
    /// Checks if rate exists for currency
    /// </summary>
    public bool HasCurrency(string currency) =>
        _rates.ContainsKey(currency);

    /// <summary>
    /// Checks if all requested currencies are available
    /// </summary>
    public bool HasAllCurrencies(IEnumerable<string> currencies) =>
        currencies.All(c => HasCurrency(c));

    /// <summary>
    /// Returns missing currencies from requested set
    /// </summary>
    public IEnumerable<string> GetMissingCurrencies(IEnumerable<string> requested) =>
        requested.Where(c => !HasCurrency(c)).Distinct();

    /// <summary>
    /// Checks if rates are stale (>5 minutes old)
    /// </summary>
    public bool IsStale(TimeSpan? maxAge = null)
    {
        var threshold = maxAge ?? TimeSpan.FromMinutes(5);
        return DateTimeOffset.UtcNow - FetchedAt > threshold;
    }
}
```

### Exchange Rate Error Domain Exception

```csharp
namespace Umbraco.Community.LightningPayments.Core.Domain.Exceptions;

/// <summary>
/// Thrown when exchange rate service encounters errors.
/// </summary>
public sealed class ExchangeRateException : DomainException
{
    /// <summary>
    /// Error code for categorization (e.g., "RATE_LIMIT", "SERVICE_UNAVAILABLE")
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Whether error is transient (safe to retry)
    /// </summary>
    public bool IsTransient { get; }

    /// <summary>
    /// HTTP status code if from API
    /// </summary>
    public int? HttpStatusCode { get; }

    public ExchangeRateException(
        string message,
        string errorCode,
        bool isTransient = false,
        int? httpStatusCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        IsTransient = isTransient;
        HttpStatusCode = httpStatusCode;
    }
}
```

---

## 2. Application Layer - Service Interface

### IExchangeRateService

```csharp
namespace Umbraco.Community.LightningPayments.Core.Application.Services;

/// <summary>
/// Service for fetching and caching exchange rates from external sources.
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Fetches Bitcoin exchange rates for specified fiat currencies.
    /// Uses cache when available (5-minute TTL).
    /// </summary>
    /// <param name="fiatCurrencies">ISO 4217 currency codes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing exchange rates or error</returns>
    Task<Result<ExchangeRates>> GetBitcoinRatesAsync(
        IEnumerable<string> fiatCurrencies,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears cached exchange rates for specified currencies.
    /// </summary>
    /// <param name="currencies">Currencies to invalidate (null = all)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task InvalidateCacheAsync(
        IEnumerable<string>? currencies = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets list of supported currencies from the exchange rate provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of ISO 4217 currency codes</returns>
    Task<Result<IEnumerable<string>>> GetSupportedCurrenciesAsync(
        CancellationToken cancellationToken = default);
}
```

---

## 3. Infrastructure Layer - HTTP Client

### CoinGecko Client

```csharp
namespace Umbraco.Community.LightningPayments.Infrastructure.ExchangeRates;

/// <summary>
/// HTTP client for CoinGecko API v3.
/// Handles request/response serialization and error mapping.
/// </summary>
public sealed class CoinGeckoHttpClient : ICoinGeckoHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoinGeckoHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public CoinGeckoHttpClient(
        HttpClient httpClient,
        ILogger<CoinGeckoHttpClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Fetches current Bitcoin prices in specified fiat currencies.
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetSimplePriceAsync(
        IEnumerable<string> fiatCurrencies,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fiatCurrencies);

        var currenciesParam = string.Join(",", fiatCurrencies.Select(c => c.ToLowerInvariant()));

        var queryParams = new Dictionary<string, string>
        {
            { "ids", "bitcoin" },
            { "vs_currencies", currenciesParam },
            { "include_last_updated_at", "true" },
            { "precision", "8" }
        };

        var query = string.Join("&", queryParams.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        var requestUri = $"/simple/price?{query}";

        _logger.LogDebug("Fetching Bitcoin rates for currencies: {Currencies}", currenciesParam);

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            LogRateLimitHeaders(response);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "CoinGecko API error {StatusCode}: {Content}",
                    response.StatusCode, errorContent);

                var isTransient = response.StatusCode is
                    System.Net.HttpStatusCode.TooManyRequests or
                    System.Net.HttpStatusCode.ServiceUnavailable or
                    System.Net.HttpStatusCode.RequestTimeout;

                throw new ExchangeRateException(
                    $"CoinGecko API returned {response.StatusCode}",
                    errorCode: MapHttpStatusToErrorCode(response.StatusCode),
                    isTransient: isTransient,
                    httpStatusCode: (int)response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var rates = ParseBitcoinRates(json);

            _logger.LogDebug("Successfully fetched {Count} exchange rates", rates.Count);

            return rates;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Network error communicating with CoinGecko API");
            throw new ExchangeRateException(
                $"Network error: {ex.Message}",
                "NETWORK_ERROR",
                isTransient: true,
                innerException: ex);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "CoinGecko API request timed out");
            throw new ExchangeRateException(
                "Exchange rate fetch timed out",
                "TIMEOUT",
                isTransient: true,
                innerException: ex);
        }
        catch (ExchangeRateException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching exchange rates");
            throw new ExchangeRateException(
                $"Unexpected error: {ex.Message}",
                "UNEXPECTED_ERROR",
                isTransient: false,
                innerException: ex);
        }
    }

    /// <summary>
    /// Fetches list of supported fiat currencies.
    /// </summary>
    public async Task<IEnumerable<string>> GetSupportedCurrenciesAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching supported currencies from CoinGecko");

        try
        {
            using var response = await _httpClient.GetAsync(
                "/simple/supported_vs_currencies",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ExchangeRateException(
                    $"Failed to fetch supported currencies: {response.StatusCode}",
                    "FETCH_CURRENCIES_FAILED",
                    isTransient: response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable,
                    httpStatusCode: (int)response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var currencies = JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new();

            _logger.LogDebug("Retrieved {Count} supported currencies", currencies.Count);

            return currencies;
        }
        catch (ExchangeRateException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching supported currencies");
            throw new ExchangeRateException(
                $"Error fetching supported currencies: {ex.Message}",
                "UNEXPECTED_ERROR",
                isTransient: false,
                innerException: ex);
        }
    }

    private static Dictionary<string, decimal> ParseBitcoinRates(string json)
    {
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("bitcoin", out var bitcoinData))
            {
                throw new ExchangeRateException(
                    "Response missing 'bitcoin' property",
                    "INVALID_RESPONSE",
                    isTransient: false);
            }

            foreach (var property in bitcoinData.EnumerateObject())
            {
                // Skip metadata fields
                if (property.Name == "last_updated_at" || property.Name == "usd_market_cap")
                    continue;

                if (property.Value.TryGetDecimal(out var rate))
                {
                    rates[property.Name] = rate;
                }
            }

            return rates;
        }
        catch (JsonException ex)
        {
            throw new ExchangeRateException(
                $"Failed to parse CoinGecko response: {ex.Message}",
                "PARSE_ERROR",
                isTransient: false,
                innerException: ex);
        }
    }

    private void LogRateLimitHeaders(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("x-ratelimit-limit-minute", out var limitValues) &&
            response.Headers.TryGetValues("x-ratelimit-remaining-minute", out var remainingValues))
        {
            var limit = limitValues.FirstOrDefault() ?? "unknown";
            var remaining = remainingValues.FirstOrDefault() ?? "unknown";

            if (int.TryParse(remaining, out var remainingCount) &&
                int.TryParse(limit, out var limitCount) &&
                remainingCount < limitCount / 10)
            {
                _logger.LogWarning(
                    "CoinGecko rate limit pressure: {Remaining}/{Limit} remaining",
                    remaining, limit);
            }
            else
            {
                _logger.LogDebug(
                    "CoinGecko rate limit: {Remaining}/{Limit} remaining",
                    remaining, limit);
            }
        }
    }

    private static string MapHttpStatusToErrorCode(System.Net.HttpStatusCode statusCode) =>
        statusCode switch
        {
            System.Net.HttpStatusCode.TooManyRequests => "RATE_LIMIT",
            System.Net.HttpStatusCode.BadRequest => "INVALID_REQUEST",
            System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden => "AUTH_ERROR",
            System.Net.HttpStatusCode.NotFound => "ENDPOINT_NOT_FOUND",
            System.Net.HttpStatusCode.ServiceUnavailable => "SERVICE_UNAVAILABLE",
            System.Net.HttpStatusCode.RequestTimeout => "TIMEOUT",
            _ => "API_ERROR"
        };
}
```

### CoinGecko HTTP Client Interface

```csharp
namespace Umbraco.Community.LightningPayments.Infrastructure.ExchangeRates;

/// <summary>
/// Low-level HTTP client for CoinGecko API communication.
/// </summary>
public interface ICoinGeckoHttpClient
{
    /// <summary>
    /// Fetches Bitcoin prices in specified fiat currencies.
    /// </summary>
    Task<Dictionary<string, decimal>> GetSimplePriceAsync(
        IEnumerable<string> fiatCurrencies,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches supported fiat currencies.
    /// </summary>
    Task<IEnumerable<string>> GetSupportedCurrenciesAsync(
        CancellationToken cancellationToken = default);
}
```

---

## 4. Application Service Implementation

### CoinGeckoExchangeRateService

```csharp
namespace Umbraco.Community.LightningPayments.Core.Application.Services.ExchangeRates;

/// <summary>
/// Application service for exchange rate operations with caching and resilience.
/// Implements IExchangeRateService with production-grade error handling.
/// </summary>
public sealed class CoinGeckoExchangeRateService : IExchangeRateService
{
    private readonly ICoinGeckoHttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CoinGeckoExchangeRateService> _logger;
    private readonly CoinGeckoOptions _options;
    private readonly ResiliencePipeline<Dictionary<string, decimal>> _resiliencePipeline;

    public CoinGeckoExchangeRateService(
        ICoinGeckoHttpClient httpClient,
        IDistributedCache cache,
        ILogger<CoinGeckoExchangeRateService> logger,
        IOptions<CoinGeckoOptions> options,
        ResiliencePipeline<Dictionary<string, decimal>>? resiliencePipeline = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _resiliencePipeline = resiliencePipeline ?? CreateDefaultResiliencePipeline();
    }

    public async Task<Result<ExchangeRates>> GetBitcoinRatesAsync(
        IEnumerable<string> fiatCurrencies,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fiatCurrencies);

        var currenciesList = fiatCurrencies.Select(c => c.ToUpperInvariant()).Distinct().ToList();

        if (!currenciesList.Any())
        {
            _logger.LogWarning("No currencies requested for exchange rates");
            return Result.Failure<ExchangeRates>(
                new ApplicationError("NO_CURRENCIES", "At least one currency required"));
        }

        var cacheKey = GenerateCacheKey(currenciesList);

        try
        {
            // Try cache first
            if (await GetCachedRatesAsync(cacheKey, currenciesList, cancellationToken)
                is { IsSuccess: true } cachedResult)
            {
                _logger.LogDebug("Exchange rates served from cache");
                return cachedResult;
            }

            // Fetch from API with resilience
            var rates = await _resiliencePipeline.ExecuteAsync(
                async _ => await _httpClient.GetSimplePriceAsync(currenciesList, cancellationToken),
                ResilienceContextPool.Shared.Get());

            var exchangeRates = BuildExchangeRates(rates, currenciesList);

            // Cache the result
            await CacheRatesAsync(cacheKey, exchangeRates, cancellationToken);

            return Result.Success(exchangeRates);
        }
        catch (ExchangeRateException ex) when (ex.IsTransient)
        {
            _logger.LogWarning(ex, "Transient error fetching rates, attempting cache fallback");

            // Try stale cache on transient error
            if (await GetCachedRatesAsync(cacheKey, currenciesList, cancellationToken, allowStale: true)
                is { IsSuccess: true } staleCachedResult)
            {
                return staleCachedResult;
            }

            return Result.Failure<ExchangeRates>(
                new ApplicationError("RATES_UNAVAILABLE", ex.Message));
        }
        catch (ExchangeRateException ex)
        {
            _logger.LogError(ex, "Permanent error fetching exchange rates");
            return Result.Failure<ExchangeRates>(
                new ApplicationError("RATES_ERROR", ex.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Exchange rate fetch cancelled");
            return Result.Failure<ExchangeRates>(
                new ApplicationError("CANCELLED", "Exchange rate fetch was cancelled"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching exchange rates");
            return Result.Failure<ExchangeRates>(
                new ApplicationError("UNEXPECTED", "Unexpected error fetching exchange rates"));
        }
    }

    public async Task InvalidateCacheAsync(
        IEnumerable<string>? currencies = null,
        CancellationToken cancellationToken = default)
    {
        if (currencies == null)
        {
            _logger.LogInformation("Invalidating all cached exchange rates");
            // For distributed cache, you'd need to implement a pattern or use a cache key prefix
            return;
        }

        var currenciesList = currencies.Select(c => c.ToUpperInvariant()).Distinct().ToList();
        var cacheKey = GenerateCacheKey(currenciesList);

        await _cache.RemoveAsync(cacheKey, cancellationToken);
        _logger.LogInformation("Invalidated cache for currencies: {Currencies}", string.Join(",", currenciesList));
    }

    public async Task<Result<IEnumerable<string>>> GetSupportedCurrenciesAsync(
        CancellationToken cancellationToken = default)
    {
        const string cacheKey = "coingecko:supported_currencies";

        try
        {
            // Check cache first (24-hour TTL for static data)
            var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cached))
            {
                var currencies = JsonSerializer.Deserialize<List<string>>(cached);
                if (currencies != null)
                {
                    _logger.LogDebug("Supported currencies served from cache");
                    return Result.Success<IEnumerable<string>>(currencies);
                }
            }

            // Fetch from API
            var currencies_fresh = await _httpClient.GetSupportedCurrenciesAsync(cancellationToken);

            // Cache for 24 hours
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(currencies_fresh),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                },
                cancellationToken);

            return Result.Success(currencies_fresh);
        }
        catch (ExchangeRateException ex)
        {
            _logger.LogError(ex, "Error fetching supported currencies");
            return Result.Failure<IEnumerable<string>>(
                new ApplicationError("CURRENCIES_ERROR", ex.Message));
        }
    }

    private async Task<Result<ExchangeRates>?> GetCachedRatesAsync(
        string cacheKey,
        IEnumerable<string> requestedCurrencies,
        CancellationToken cancellationToken,
        bool allowStale = false)
    {
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (string.IsNullOrEmpty(cached))
            return null;

        var rates = JsonSerializer.Deserialize<ExchangeRates>(cached);
        if (rates == null)
            return null;

        if (!allowStale && rates.IsStale(_options.CacheTtl))
        {
            _logger.LogDebug("Cached exchange rates are stale, will refresh");
            return null;
        }

        if (!rates.HasAllCurrencies(requestedCurrencies))
        {
            _logger.LogWarning("Cached rates missing some requested currencies");
            return null;
        }

        return Result.Success(rates);
    }

    private async Task CacheRatesAsync(
        string cacheKey,
        ExchangeRates rates,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(rates);
            await _cache.SetStringAsync(
                cacheKey,
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _options.CacheTtl
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache exchange rates (non-blocking)");
            // Don't fail the operation if cache fails
        }
    }

    private static ExchangeRates BuildExchangeRates(
        Dictionary<string, decimal> ratesByCode,
        IEnumerable<string> requestedCurrencies)
    {
        var rates = requestedCurrencies
            .Where(c => ratesByCode.TryGetValue(c, out _))
            .Select(c => ExchangeRate.Create(
                currency: c,
                ratePerBitcoin: ratesByCode[c],
                fetchedAt: DateTimeOffset.UtcNow,
                isFromCache: false))
            .ToList();

        return new ExchangeRates(rates, DateTimeOffset.UtcNow);
    }

    private static string GenerateCacheKey(IEnumerable<string> currencies)
    {
        var sorted = string.Join(",", currencies.OrderBy(c => c));
        return $"coingecko:rates:{sorted}";
    }

    private static ResiliencePipeline<Dictionary<string, decimal>> CreateDefaultResiliencePipeline()
    {
        return new ResiliencePipelineBuilder<Dictionary<string, decimal>>()
            .AddRetry(new RetryStrategyOptions<Dictionary<string, decimal>>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(50),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is HttpRequestException or TimeoutException or
                    OperationCanceledException)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<Dictionary<string, decimal>>
            {
                FailureRatio = 0.5,
                MinimumThroughput = 3,
                SamplingDuration = TimeSpan.FromSeconds(10),
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is HttpRequestException or
                    (args.Outcome.Result == null))
            })
            .Build();
    }
}
```

---

## 5. Configuration & Dependency Injection

### CoinGeckoOptions

```csharp
namespace Umbraco.Community.LightningPayments.Infrastructure.Configuration;

/// <summary>
/// Configuration options for CoinGecko API integration.
/// </summary>
public sealed class CoinGeckoOptions
{
    /// <summary>
    /// Base URL for CoinGecko API
    /// </summary>
    [Url]
    public string BaseUrl { get; set; } = "https://api.coingecko.com/api/v3";

    /// <summary>
    /// Optional Pro API key (enables higher rate limits)
    /// </summary>
    public string? ProApiKey { get; set; }

    /// <summary>
    /// HTTP request timeout (recommended: 10 seconds)
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Exchange rate cache TTL (recommended: 5 minutes)
    /// </summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Validates configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
            throw new InvalidOperationException("CoinGecko BaseUrl cannot be empty");

        if (Timeout <= TimeSpan.Zero)
            throw new InvalidOperationException("CoinGecko Timeout must be positive");

        if (CacheTtl <= TimeSpan.Zero)
            throw new InvalidOperationException("CoinGecko CacheTtl must be positive");
    }
}
```

### Composition Root Setup

```csharp
namespace Umbraco.Community.LightningPayments.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for registering CoinGecko services
/// </summary>
public static class CoinGeckoServiceCollectionExtensions
{
    /// <summary>
    /// Adds CoinGecko exchange rate services to the DI container
    /// </summary>
    public static IServiceCollection AddCoinGeckoExchangeRates(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure options
        services
            .AddOptions<CoinGeckoOptions>()
            .Bind(configuration.GetSection("CoinGecko"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register HttpClient with resilience
        services
            .AddHttpClient<ICoinGeckoHttpClient, CoinGeckoHttpClient>()
            .ConfigureHttpClient((provider, httpClient) =>
            {
                var options = provider.GetRequiredService<IOptions<CoinGeckoOptions>>().Value;

                httpClient.BaseAddress = new Uri(options.BaseUrl);
                httpClient.Timeout = options.Timeout;

                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Lightning-Payments/1.0");

                // Add Pro API key if configured
                if (!string.IsNullOrEmpty(options.ProApiKey))
                {
                    httpClient.DefaultRequestHeaders.Add("x-cg-pro-api-key", options.ProApiKey);
                }
            })
            .ConfigureHttpMessageHandler(_ => new SocketsHttpHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip |
                    System.Net.DecompressionMethods.Deflate,
                UseCookies = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            })
            .AddPolicyHandler(ResiliencePolicies.GetRetryPolicy())
            .AddPolicyHandler(ResiliencePolicies.GetCircuitBreakerPolicy());

        // Register application service
        services.AddScoped<IExchangeRateService, CoinGeckoExchangeRateService>();

        return services;
    }
}
```

### Resilience Policy Factory

```csharp
namespace Umbraco.Community.LightningPayments.Infrastructure.Resilience;

/// <summary>
/// Factory for creating resilience policies for external API communication
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Creates retry policy with exponential backoff
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<HttpRequestException>()
            .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 50), // 50ms, 100ms, 200ms
                onRetry: (outcome, timeSpan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning(
                        "Retry {RetryCount} after {Delay}ms due to {Reason}",
                        retryCount, timeSpan.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    /// <summary>
    /// Creates circuit breaker policy
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogError(
                        "Circuit breaker opened for {Duration}ms due to {Reason}",
                        duration.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }
}
```

### appsettings.json

```json
{
  "CoinGecko": {
    "BaseUrl": "https://api.coingecko.com/api/v3",
    "ProApiKey": null,
    "Timeout": "PT10S",
    "CacheTtl": "PT5M"
  }
}
```

### appsettings.Production.json

```json
{
  "CoinGecko": {
    "ProApiKey": "${COINGECKO_PRO_API_KEY}"
  }
}
```

---

## 6. Usage Examples

### In a Paywall Controller

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public sealed class PaywallController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IPaywallService _paywallService;
    private readonly ILogger<PaywallController> _logger;

    public PaywallController(
        IExchangeRateService exchangeRateService,
        IPaywallService paywallService,
        ILogger<PaywallController> logger)
    {
        _exchangeRateService = exchangeRateService;
        _paywallService = paywallService;
        _logger = logger;
    }

    /// <summary>
    /// Gets paywall information including fiat conversion
    /// </summary>
    [HttpGet("{contentId}")]
    public async Task<ActionResult<PaywallDisplayDto>> GetPaywall(
        Guid contentId,
        string? currency = "USD",
        CancellationToken cancellationToken = default)
    {
        var paywall = await _paywallService.GetPaywallAsync(contentId, cancellationToken);
        if (paywall == null)
            return NotFound();

        // Fetch exchange rates
        var ratesResult = await _exchangeRateService.GetBitcoinRatesAsync(
            new[] { currency ?? "USD" },
            cancellationToken);

        var response = new PaywallDisplayDto
        {
            ContentId = contentId,
            AmountSat = paywall.AmountSat,
            Currency = currency ?? "USD"
        };

        if (ratesResult.IsSuccess)
        {
            var rates = ratesResult.Value;
            var rateResult = rates.GetRate(currency ?? "USD");

            if (rateResult.IsSuccess)
            {
                response.AmountFiat = rateResult.Value.ConvertFromSatoshi(paywall.AmountSat);
                response.IsFromCache = rates.IsFromCache;
            }
        }
        else
        {
            _logger.LogWarning(
                "Failed to fetch exchange rates: {Error}",
                ratesResult.Error?.Message);
        }

        return Ok(response);
    }
}
```

### In Admin Configuration

```csharp
[ApiController]
[Route("api/v1/admin/[controller]")]
[Authorize(Roles = "Administrator")]
public sealed class ExchangeRatesAdminController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<ExchangeRatesAdminController> _logger;

    public ExchangeRatesAdminController(
        IExchangeRateService exchangeRateService,
        ILogger<ExchangeRatesAdminController> logger)
    {
        _exchangeRateService = exchangeRateService;
        _logger = logger;
    }

    /// <summary>
    /// Gets supported currencies for configuration UI
    /// </summary>
    [HttpGet("supported-currencies")]
    public async Task<ActionResult<IEnumerable<string>>> GetSupportedCurrencies(
        CancellationToken cancellationToken = default)
    {
        var result = await _exchangeRateService.GetSupportedCurrenciesAsync(cancellationToken);

        if (result.IsSuccess)
            return Ok(result.Value.OrderBy(c => c));

        _logger.LogError("Failed to fetch supported currencies: {Error}", result.Error?.Message);
        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new { error = "Exchange rate service unavailable" });
    }

    /// <summary>
    /// Manually invalidates exchange rate cache
    /// </summary>
    [HttpPost("cache/invalidate")]
    public async Task<ActionResult> InvalidateCache(
        [FromBody] InvalidateCacheRequest request,
        CancellationToken cancellationToken = default)
    {
        await _exchangeRateService.InvalidateCacheAsync(request.Currencies, cancellationToken);

        _logger.LogInformation(
            "Exchange rate cache invalidated for currencies: {Currencies}",
            string.Join(",", request.Currencies ?? Array.Empty<string>()));

        return Ok();
    }
}

public record InvalidateCacheRequest
{
    public string[]? Currencies { get; init; }
}
```

---

## 7. Testing

### Unit Test Example

```csharp
namespace Umbraco.Community.LightningPayments.Tests.Infrastructure.ExchangeRates;

public sealed class CoinGeckoExchangeRateServiceTests
{
    private readonly Mock<ICoinGeckoHttpClient> _httpClientMock;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CoinGeckoExchangeRateService> _logger;
    private readonly IOptions<CoinGeckoOptions> _options;
    private readonly CoinGeckoExchangeRateService _service;

    public CoinGeckoExchangeRateServiceTests()
    {
        _httpClientMock = new Mock<ICoinGeckoHttpClient>();
        _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _logger = new NullLogger<CoinGeckoExchangeRateService>();
        _options = Options.Create(new CoinGeckoOptions
        {
            CacheTtl = TimeSpan.FromMinutes(5)
        });

        _service = new CoinGeckoExchangeRateService(
            _httpClientMock.Object,
            _cache,
            _logger,
            _options);
    }

    [Fact]
    public async Task GetBitcoinRatesAsync_WithValidCurrencies_ReturnRates()
    {
        // Arrange
        var rates = new Dictionary<string, decimal>
        {
            { "USD", 42500.50m },
            { "EUR", 39200.25m }
        };

        _httpClientMock
            .Setup(x => x.GetSimplePriceAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates);

        // Act
        var result = await _service.GetBitcoinRatesAsync(new[] { "USD", "EUR" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Rates.Count);
        Assert.Equal(42500.50m, result.Value.Rates["USD"].RatePerBitcoin);
    }

    [Fact]
    public async Task GetBitcoinRatesAsync_OnTransientError_ReturnsCachedRate()
    {
        // Arrange - seed cache
        var rates = new Dictionary<string, decimal> { { "USD", 42500.50m } };
        _httpClientMock
            .SetupSequence(x => x.GetSimplePriceAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rates)
            .ThrowsAsync(new ExchangeRateException("Network error", "NETWORK_ERROR", isTransient: true));

        // First call succeeds and caches
        await _service.GetBitcoinRatesAsync(new[] { "USD" });

        // Act - second call fails but should return cached
        var result = await _service.GetBitcoinRatesAsync(new[] { "USD" });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsFromCache);
    }
}
```

---

## Summary

This implementation provides:

1. **Domain Models**: Type-safe value objects for exchange rates
2. **Service Layer**: Application service with caching and resilience
3. **HTTP Client**: Production-grade CoinGecko integration
4. **Configuration**: Externalized settings with validation
5. **Error Handling**: Comprehensive exception mapping and recovery
6. **Caching**: Distributed cache with stale fallback
7. **Testing**: Mockable interfaces for unit testing
8. **Observability**: Logging and metrics support

All code follows constitutional principles and .NET best practices.

