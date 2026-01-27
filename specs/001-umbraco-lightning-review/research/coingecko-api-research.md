# CoinGecko API Research: Bitcoin Exchange Rate Integration

**Document ID**: CoinGecko-Research-001
**Date**: 2026-01-26
**Status**: Research Complete
**Target Requirements**: FR-019, FR-020, FR-021, FR-022

## Executive Summary

CoinGecko provides free, reliable access to cryptocurrency exchange rate data via a straightforward REST API. For the multi-currency display feature (FR-019 through FR-022), the `/simple/price` endpoint is ideal—it requires minimal dependencies, handles multiple currencies in a single request, and includes standard rate limit accommodations suitable for production systems.

**Key Recommendation**: Use free tier (10-50 requests/minute) with 5-minute cache TTL. For high-traffic deployments, upgrade to Pro tier (50 requests/minute with 100% uptime SLA).

---

## 1. CoinGecko API Endpoints

### Primary Endpoint: `/simple/price`

**URL**: `https://api.coingecko.com/api/v3/simple/price`

**Purpose**: Fetch current cryptocurrency prices in multiple fiat currencies with minimal latency.

**HTTP Method**: `GET`

**Query Parameters**:

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `ids` | string | Yes | Comma-separated cryptocurrency IDs | `bitcoin` |
| `vs_currencies` | string | Yes | Comma-separated target currencies (ISO 4217 codes) | `usd,eur,gbp` |
| `include_market_cap` | boolean | No | Include market cap data | `false` (default) |
| `include_24hr_vol` | boolean | No | Include 24-hour volume | `false` (default) |
| `include_last_updated_at` | boolean | No | Include Unix timestamp of last update | `true` (recommended) |
| `precision` | integer | No | Decimal precision for response (1-18) | `2` for standard currency |

**Example Request**:

```http
GET /api/v3/simple/price?ids=bitcoin&vs_currencies=usd,eur,gbp&include_last_updated_at=true&precision=2
Host: api.coingecko.com
```

**Response (Success - 200 OK)**:

```json
{
  "bitcoin": {
    "usd": 42500.50,
    "eur": 39200.25,
    "gbp": 33800.75,
    "last_updated_at": 1706275200
  }
}
```

**Response Structure**:
- Root object key = cryptocurrency ID (`bitcoin`)
- Each currency code = exchange rate (decimal number)
- `last_updated_at` (optional) = Unix timestamp when rate was fetched from exchange

### Alternative: `/simple/price` with Market Data

If you need comprehensive market data alongside prices:

```http
GET /api/v3/simple/price?ids=bitcoin&vs_currencies=usd&include_market_cap=true&include_24hr_vol=true
Host: api.coingecko.com
```

**Response**:

```json
{
  "bitcoin": {
    "usd": 42500.50,
    "usd_market_cap": 831455000000,
    "usd_24h_vol": 28500000000
  }
}
```

### Secondary Endpoint: `/simple/supported_vs_currencies`

**URL**: `https://api.coingecko.com/api/v3/simple/supported_vs_currencies`

**Purpose**: List all supported fiat currencies (for UI currency selector).

**Response**: JSON array of ISO 4217 currency codes

```json
[
  "usd",
  "eur",
  "gbp",
  "jpy",
  "aud",
  "cad",
  "chf",
  "cny",
  "inr",
  "krw",
  "zar",
  ...
]
```

**Use Case**: Pre-populate currency dropdown in Umbraco backoffice settings.

---

## 2. Free vs Pro API Tiers

### Free Tier

**Rate Limit**: ~10-50 requests per minute (non-guaranteed)

**Characteristics**:
- No authentication required
- No uptime SLA
- Best for low-to-medium traffic sites
- Suitable for most production deployments
- May experience occasional throttling during peak traffic

**Cost**: Free

**Recommended For**:
- Single-site deployments
- Content sites with <10K daily visitors
- Internal/test environments

### Pro Tier

**Rate Limit**: Guaranteed 50 requests per minute

**Characteristics**:
- Requires API key authentication (`x-cg-pro-api-key` header)
- 99.9% uptime SLA
- Priority support
- Higher reliability during traffic spikes
- Useful for monitoring and analytics

**Cost**: Varies (check https://www.coingecko.com/en/api/pricing)

**Recommended For**:
- High-traffic sites (>50K daily visitors)
- Mission-critical payment systems requiring guaranteed uptime
- Enterprise deployments with SLA requirements

### Upgrade Path

**Strategy**: Start with free tier, monitor rate limit headers, upgrade if needed:

```csharp
// Monitor these response headers
string remaining = response.Headers.GetValues("x-ratelimit-remaining-minute").FirstOrDefault();
string limit = response.Headers.GetValues("x-ratelimit-limit-minute").FirstOrDefault();

// If remaining consistently < 10% of limit, consider upgrade
if (int.Parse(remaining) < int.Parse(limit) * 0.1m)
{
    _logger.LogWarning("CoinGecko rate limit pressure: {Remaining}/{Limit}", remaining, limit);
    // Alert operations team to consider Pro tier
}
```

---

## 3. Response Format & Data Structure

### Bitcoin Price Response (Standard)

```json
{
  "bitcoin": {
    "usd": 42500.50,
    "eur": 39200.25,
    "gbp": 33800.75,
    "jpy": 5845000.00,
    "last_updated_at": 1706275200
  }
}
```

**Field Types**:
- Price values: `decimal` (up to 8 decimal places)
- Timestamps: `long` (Unix epoch seconds)
- Market cap/volume: `long` (satoshi for crypto, fiat for currencies)

### Error Responses

**429 Too Many Requests** (Rate limited):

```json
{
  "status": {
    "error_code": 1002,
    "error_message": "You've reached the Rate limit. Please visit https://www.coingecko.com/en/api/pricing to subscribe to a plan."
  }
}
```

**400 Bad Request** (Invalid parameters):

```json
{
  "status": {
    "error_code": 1006,
    "error_message": "invalid vs_currencies"
  }
}
```

**500 Internal Server Error** (CoinGecko API issue):

```json
{
  "status": {
    "error_code": 1000,
    "error_message": "Internal server error"
  }
}
```

### Key Characteristics for .NET Integration

1. **Decimal Precision**: Bitcoin prices against fiat currencies use 2-8 decimal places
   - USD: typically 2 decimals (e.g., `42500.50`)
   - Smaller currencies (JPY, KRW): 0 decimals
   - Crypto pairs: 8 decimals

2. **Timestamp Format**: Unix epoch seconds (not ISO 8601)
   - Convert with: `DateTimeOffset.FromUnixTimeSeconds(value)`

3. **Null Handling**: Missing currencies in response indicate API error or invalid parameter
   - Always validate response contains all requested currencies

4. **No Pagination**: `/simple/price` returns all requested currencies in single response

---

## 4. Caching Strategy

### Recommended Cache Pattern

**Cache Duration**: 5 minutes per specification (FR-021)

**Rationale**:
- Bitcoin price changes frequently (intraday volatility 2-5%)
- 5-minute staleness acceptable for display purposes (users won't refresh faster)
- Reduces API calls from ~1,000/day to ~288/day (5-minute granularity)
- Well within free tier limits

**Cache Key Structure**:

```csharp
// Example: "coingecko:bitcoin:usd,eur,gbp"
string cacheKey = $"coingecko:{cryptoId}:{string.Join(",", currencies)}";
```

### Implementation Pattern in .NET

```csharp
/// <summary>
/// Fetches Bitcoin exchange rates with intelligent caching.
/// </summary>
public async Task<ExchangeRates> GetBitcoinRatesAsync(
    IEnumerable<string> fiatCurrencies,
    CancellationToken cancellationToken = default)
{
    var currenciesKey = string.Join(",", fiatCurrencies.OrderBy(c => c));
    var cacheKey = $"coingecko:bitcoin:{currenciesKey}";

    // Try cache first
    if (_cache.TryGetValue(cacheKey, out ExchangeRates? cachedRates))
    {
        _logger.LogDebug("Cache hit for exchange rates: {CurrencyKey}", currenciesKey);
        return cachedRates!;
    }

    // Fetch from API
    var rates = await _httpClient.GetBitcoinRatesAsync(fiatCurrencies, cancellationToken);

    // Cache with 5-minute TTL
    var cacheOptions = new MemoryCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        SlidingExpiration = TimeSpan.FromMinutes(2) // Refresh if accessed within 5 min
    };

    _cache.Set(cacheKey, rates, cacheOptions);

    return rates;
}
```

### Cache Invalidation Triggers

**Explicit invalidation** when:
- Settings change (display currency updated)
- Manual refresh requested by admin
- API returns error (let cache expire naturally)

**Code Example**:

```csharp
public async Task InvalidateExchangeRatesAsync(string? currencyKey = null)
{
    if (currencyKey != null)
    {
        _cache.Remove($"coingecko:bitcoin:{currencyKey}");
    }
    else
    {
        // Clear all cached rates
        _cache.Remove("coingecko:bitcoin:*");
    }
}
```

### Distributed Cache Consideration

For multi-server deployments, use **distributed cache** (Redis/SQL Server):

```csharp
// IDistributedCache from Microsoft.Extensions.Caching.StackExchangeRedis
public async Task<ExchangeRates> GetBitcoinRatesAsync(
    IEnumerable<string> fiatCurrencies,
    CancellationToken cancellationToken = default)
{
    var cacheKey = GenerateCacheKey(fiatCurrencies);

    var cached = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
    if (cached != null)
    {
        return JsonSerializer.Deserialize<ExchangeRates>(cached)!;
    }

    var rates = await FetchRatesAsync(fiatCurrencies, cancellationToken);

    await _distributedCache.SetStringAsync(
        cacheKey,
        JsonSerializer.Serialize(rates),
        new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        },
        cancellationToken);

    return rates;
}
```

---

## 5. Error Handling & Fallback Approaches

### Error Categories & Responses

#### Category 1: Transient Errors (Retry-Eligible)

| Error | HTTP Code | Cause | Strategy |
|-------|-----------|-------|----------|
| Rate Limited | 429 | Too many requests | Exponential backoff + Circuit breaker |
| Temporary Outage | 503 | CoinGecko maintenance | Retry with backoff |
| Connection Timeout | TCP/Network | Network issue | Exponential backoff |
| DNS Resolution | Network | DNS issue | Exponential backoff |

#### Category 2: Permanent Errors (Don't Retry)

| Error | HTTP Code | Cause | Strategy |
|-------|-----------|-------|----------|
| Invalid Currency | 400 | Typo in parameter | Log error, use cached value |
| Invalid Endpoint | 404 | API changed | Alert ops, use cached value |
| Authentication Fail | 401/403 | Invalid Pro API key | Log + use cached value |

#### Category 3: Degradation Errors (Fallback Only)

| Error | Cause | Strategy |
|-------|-------|----------|
| Cache Miss + API Fail | All services down | Return satoshi-only display |
| Partial Response | Some currencies missing | Return available rates + warn user |

### Resilience Policy Pattern (Polly)

```csharp
public sealed class CoinGeckoResiliencePolicies
{
    /// <summary>
    /// Retry policy: 3 exponential retries (50ms, 100ms, 200ms base)
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateRetryPolicy()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(50),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception is HttpRequestException ||
                    (args.Outcome.Result?.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable) ||
                    (args.Outcome.Result?.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                )
            })
            .Build();
    }

    /// <summary>
    /// Circuit breaker: Opens after 3 failures, half-open after 10 seconds
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateCircuitBreakerPolicy()
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5, // Open if >50% fail
                MinimumThroughput = 3,
                SamplingDuration = TimeSpan.FromSeconds(10),
                BreakDuration = TimeSpan.FromSeconds(10),
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Exception != null ||
                    (args.Outcome.Result?.IsSuccessStatusCode == false)
                )
            })
            .Build();
    }
}
```

### Error Handling in Service Layer

```csharp
public sealed class CoinGeckoExchangeRateService : IExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CoinGeckoExchangeRateService> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _resilience;

    public async Task<Result<ExchangeRates>> GetBitcoinRatesAsync(
        IEnumerable<string> fiatCurrencies,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rates = await FetchOrCacheRatesAsync(fiatCurrencies, cancellationToken);
            return Result.Success(rates);
        }
        catch (CoinGeckoRateLimitedException ex)
        {
            _logger.LogWarning(
                ex,
                "CoinGecko rate limit exceeded. Remaining quota: {Remaining}",
                ex.RemainingRequests);

            // Return cached rates if available
            var cached = GetCachedRates(fiatCurrencies);
            return cached != null
                ? Result.Success(cached)
                : Result.Failure<ExchangeRates>(
                    new ExchangeRateError("RATE_LIMIT", "Exchange rate service temporarily unavailable"));
        }
        catch (CoinGeckoServiceException ex) when (ex.IsTransient)
        {
            _logger.LogWarning(ex, "Transient CoinGecko API error, using cached rates");

            var cached = GetCachedRates(fiatCurrencies);
            return cached != null
                ? Result.Success(cached)
                : Result.Failure<ExchangeRates>(
                    new ExchangeRateError("SERVICE_UNAVAILABLE", "Exchange rate service temporarily unavailable"));
        }
        catch (CoinGeckoServiceException ex)
        {
            _logger.LogError(ex, "Permanent CoinGecko API error");
            return Result.Failure<ExchangeRates>(
                new ExchangeRateError("API_ERROR", ex.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Exchange rate fetch cancelled (timeout)");
            return Result.Failure<ExchangeRates>(
                new ExchangeRateError("TIMEOUT", "Exchange rate fetch timed out"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching exchange rates");
            return Result.Failure<ExchangeRates>(
                new ExchangeRateError("UNEXPECTED", "Unexpected error fetching exchange rates"));
        }
    }

    private async Task<ExchangeRates> FetchOrCacheRatesAsync(
        IEnumerable<string> fiatCurrencies,
        CancellationToken cancellationToken)
    {
        var cacheKey = GenerateCacheKey(fiatCurrencies);

        if (_cache.TryGetValue(cacheKey, out ExchangeRates? cached))
        {
            return cached!;
        }

        var rates = await ExecuteWithResilienceAsync(fiatCurrencies, cancellationToken);

        _cache.Set(cacheKey, rates, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return rates;
    }

    private async Task<ExchangeRates> ExecuteWithResilienceAsync(
        IEnumerable<string> fiatCurrencies,
        CancellationToken cancellationToken)
    {
        var outcome = await _resilience.ExecuteAsync(
            async _ => await FetchRatesFromApiAsync(fiatCurrencies, cancellationToken),
            ResilienceContextPool.Shared.Get());

        return outcome.Result ?? throw outcome.Exception!;
    }
}
```

### Graceful Degradation UI Display

```csharp
/// <summary>
/// Represents exchange rate display with fallback capabilities.
/// </summary>
public record ExchangeRateDisplay
{
    /// <summary>
    /// Bitcoin amount in satoshis
    /// </summary>
    public long AmountSat { get; init; }

    /// <summary>
    /// Fiat amount (nullable if unavailable)
    /// </summary>
    public decimal? AmountFiat { get; init; }

    /// <summary>
    /// Display currency code (e.g., "USD", "EUR")
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// Whether fiat amount is from cache or current rate
    /// </summary>
    public bool IsFromCache { get; init; }

    /// <summary>
    /// User-facing message if rate unavailable
    /// </summary>
    public string? UnavailabilityReason { get; init; }
}
```

**Usage in Controller**:

```csharp
[HttpGet("paywall/{pageId}")]
public async Task<ActionResult<PaywallDisplayDto>> GetPaywallDisplay(
    Guid pageId,
    string? currencyOverride = null,
    CancellationToken cancellationToken = default)
{
    var paywall = await _paywallService.GetPaywallAsync(pageId, cancellationToken);
    var currency = currencyOverride ?? _settings.DefaultCurrency;

    var ratesResult = await _exchangeRateService.GetBitcoinRatesAsync(
        new[] { currency },
        cancellationToken);

    var fiatAmount = ratesResult.IsSuccess
        ? paywall.AmountSat * ratesResult.Value.Rates[currency]
        : null;

    return Ok(new PaywallDisplayDto
    {
        AmountSat = paywall.AmountSat,
        AmountFiat = fiatAmount,
        Currency = currency,
        IsFromCache = ratesResult.IsSuccess && ratesResult.Value.IsFromCache,
        UnavailabilityReason = ratesResult.IsFailure
            ? "Exchange rates unavailable - showing BTC amount only"
            : null
    });
}
```

---

## 6. Pro API Upgrade Path

### Transition Strategy

**Phase 1: Monitor Free Tier**
- Track rate limit headers: `x-ratelimit-limit-minute`, `x-ratelimit-remaining-minute`
- Log warnings when remaining < 10 per minute
- Maintain uptime metrics

**Phase 2: Evaluate Pro Tier**
- Calculate average daily API calls
- Compare with Pro tier cost (~$50-200/month typical)
- Assess uptime requirements (SLA vs free tier)

**Phase 3: Upgrade Implementation**

```csharp
// Configuration
public class CoinGeckoOptions
{
    public string? ProApiKey { get; set; }
    public bool UseProTier => !string.IsNullOrEmpty(ProApiKey);
}

// HTTP Client Configuration
services.AddHttpClient<ICoinGeckoClient>()
    .ConfigureHttpClient((provider, httpClient) =>
    {
        var options = provider.GetRequiredService<IOptions<CoinGeckoOptions>>().Value;

        if (options.UseProTier)
        {
            httpClient.DefaultRequestHeaders.Add("x-cg-pro-api-key", options.ProApiKey);
            _logger.LogInformation("CoinGecko Pro API enabled");
        }
        else
        {
            _logger.LogInformation("CoinGecko Free API in use");
        }
    });

// AppSettings.json configuration
{
  "CoinGecko": {
    "ProApiKey": null,  // Set from environment variable in production
    "BaseUrl": "https://api.coingecko.com/api/v3",
    "Timeout": "PT10S",
    "CacheTtl": "PT5M"
  }
}
```

**Environment Variable Setup**:

```bash
# Development (free tier)
export COINGECKO_PROAPI_KEY=

# Production (with Pro upgrade)
export COINGECKO_PROAPI_KEY=your-pro-api-key-here
```

---

## 7. HttpClient Best Practices in .NET

### HttpClientFactory Configuration

```csharp
// Startup configuration
services
    .AddHttpClient<ICoinGeckoClient, CoinGeckoClient>()
    .ConfigureHttpClient(httpClient =>
    {
        httpClient.BaseAddress = new Uri("https://api.coingecko.com/api/v3");
        httpClient.Timeout = TimeSpan.FromSeconds(10);

        // Add required headers
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Lightning-Payments/1.0");
    })
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
```

### Typed Client Implementation

```csharp
/// <summary>
/// Typed HttpClient for CoinGecko API interactions.
/// Handles serialization, error handling, and resilience.
/// </summary>
public sealed class CoinGeckoClient : ICoinGeckoClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoinGeckoClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public CoinGeckoClient(HttpClient httpClient, ILogger<CoinGeckoClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Fetches Bitcoin exchange rates for specified fiat currencies.
    /// </summary>
    /// <param name="fiatCurrencies">ISO 4217 currency codes (e.g., "usd,eur,gbp")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exchange rates map</returns>
    /// <exception cref="CoinGeckoServiceException">On API errors</exception>
    public async Task<Dictionary<string, decimal>> GetBitcoinPriceAsync(
        IEnumerable<string> fiatCurrencies,
        CancellationToken cancellationToken = default)
    {
        var currenciesParam = string.Join(",", fiatCurrencies.Select(c => c.ToLowerInvariant()));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        try
        {
            var queryParams = new Dictionary<string, string>
            {
                { "ids", "bitcoin" },
                { "vs_currencies", currenciesParam },
                { "include_last_updated_at", "true" },
                { "precision", "8" }
            };

            var query = string.Join("&", queryParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            var requestUri = $"/simple/price?{query}";

            _logger.LogDebug("Fetching Bitcoin rates for currencies: {Currencies}", currenciesParam);

            using var response = await _httpClient.GetAsync(requestUri, cts.Token);

            LogRateLimitHeaders(response);

            if (!response.IsSuccessStatusCode)
            {
                throw new CoinGeckoServiceException(
                    $"CoinGecko API error: {response.StatusCode}",
                    (int)response.StatusCode,
                    IsTransient: response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                                response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable);
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            var data = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);

            return ParseBitcoinRates(data);
        }
        catch (OperationCanceledException ex)
        {
            throw new CoinGeckoServiceException("Request timeout", 0, IsTransient: true);
        }
        catch (HttpRequestException ex)
        {
            throw new CoinGeckoServiceException(
                $"Network error: {ex.Message}",
                0,
                IsTransient: true,
                InnerException: ex);
        }
    }

    private void LogRateLimitHeaders(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("x-ratelimit-limit-minute", out var limitValues) &&
            response.Headers.TryGetValues("x-ratelimit-remaining-minute", out var remainingValues))
        {
            var limit = limitValues.FirstOrDefault() ?? "unknown";
            var remaining = remainingValues.FirstOrDefault() ?? "unknown";

            _logger.LogDebug(
                "CoinGecko rate limit: {Remaining}/{Limit} requests remaining",
                remaining, limit);
        }
    }

    private static Dictionary<string, decimal> ParseBitcoinRates(JsonElement data)
    {
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        if (!data.TryGetProperty("bitcoin", out var bitcoinData))
        {
            throw new CoinGeckoServiceException("No bitcoin data in response", 400);
        }

        foreach (var property in bitcoinData.EnumerateObject())
        {
            if (property.Name != "last_updated_at" && property.Value.TryGetDecimal(out var rate))
            {
                rates[property.Name] = rate;
            }
        }

        return rates;
    }
}
```

### Dependency Injection Setup

```csharp
// In Composition Root / Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<CoinGeckoOptions>()
    .BindConfiguration("CoinGecko")
    .ValidateDataAnnotations();

builder.Services
    .AddHttpClient<ICoinGeckoClient, CoinGeckoClient>()
    .ConfigureHttpClient((provider, httpClient) =>
    {
        var options = provider.GetRequiredService<IOptions<CoinGeckoOptions>>().Value;

        httpClient.BaseAddress = new Uri(options.BaseUrl);
        httpClient.Timeout = options.Timeout;

        if (!string.IsNullOrEmpty(options.ProApiKey))
        {
            httpClient.DefaultRequestHeaders.Add("x-cg-pro-api-key", options.ProApiKey);
        }
    })
    .ConfigureHttpMessageHandler(_ => new SocketsHttpHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
        UseCookies = false
    })
    .AddPolicyHandler(ResiliencePolicies.GetRetryPolicy())
    .AddPolicyHandler(ResiliencePolicies.GetCircuitBreakerPolicy());

builder.Services
    .AddMemoryCache()
    .AddScoped<IExchangeRateService, CoinGeckoExchangeRateService>();

var app = builder.Build();
```

### Timeout & Connection Pool Best Practices

```csharp
public class CoinGeckoOptions
{
    /// <summary>
    /// Base URL for CoinGecko API (default: production)
    /// </summary>
    [Url]
    public string BaseUrl { get; set; } = "https://api.coingecko.com/api/v3";

    /// <summary>
    /// HTTP request timeout (recommended: 10 seconds)
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Cache TTL for exchange rates (recommended: 5 minutes)
    /// </summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Optional Pro API key (enables higher rate limits)
    /// </summary>
    public string? ProApiKey { get; set; }

    /// <summary>
    /// Max connections per host (default: 10)
    /// </summary>
    public int MaxConnectionsPerHost { get; set; } = 10;
}
```

---

## 8. Production Implementation Checklist

### Before Going Live

- [ ] **Rate Limiting**: Verify cache invalidation works correctly
- [ ] **Error Handling**: Test all error paths (network, API errors, timeouts)
- [ ] **Resilience**: Verify Polly policies trigger correctly under failure
- [ ] **Logging**: Confirm correlation IDs present in logs
- [ ] **Monitoring**: Set up alerts for:
  - API response times (threshold: >2 seconds)
  - Rate limit pressure (threshold: <10 remaining/minute)
  - Circuit breaker trips (indicates persistent failures)
  - Cache hit ratio (target: >90%)
- [ ] **Security**: Verify Pro API key never logged or exposed
- [ ] **Configuration**: Test environment variable injection works
- [ ] **Integration Tests**: Mock CoinGecko API responses
- [ ] **Load Testing**: Simulate 100+ concurrent requests

### Monitoring & Observability

```csharp
public sealed class CoinGeckoMetrics
{
    private readonly Counter<int> _apiCallsTotal;
    private readonly Histogram<double> _apiCallDuration;
    private readonly Counter<int> _cacheHits;
    private readonly Counter<int> _cacheMisses;
    private readonly Counter<int> _errors;

    public CoinGeckoMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Umbraco.Community.LightningPayments.CoinGecko");

        _apiCallsTotal = meter.CreateCounter<int>(
            "coingecko.api.calls.total",
            description: "Total API calls to CoinGecko");

        _apiCallDuration = meter.CreateHistogram<double>(
            "coingecko.api.duration.ms",
            description: "API call duration in milliseconds");

        _cacheHits = meter.CreateCounter<int>(
            "coingecko.cache.hits",
            description: "Cache hits");

        _cacheMisses = meter.CreateCounter<int>(
            "coingecko.cache.misses",
            description: "Cache misses");

        _errors = meter.CreateCounter<int>(
            "coingecko.errors.total",
            description: "Total errors");
    }

    public void RecordApiCall(double durationMs)
    {
        _apiCallsTotal.Add(1);
        _apiCallDuration.Record(durationMs);
    }

    public void RecordCacheHit() => _cacheHits.Add(1);
    public void RecordCacheMiss() => _cacheMisses.Add(1);
    public void RecordError() => _errors.Add(1);
}
```

---

## 9. Example cURL Requests

### Fetch Bitcoin Prices

```bash
# Free tier - multiple currencies
curl "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd,eur,gbp&include_last_updated_at=true&precision=2"

# Pro tier with authentication
curl "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd,eur,gbp" \
  -H "x-cg-pro-api-key: YOUR_API_KEY_HERE"

# With market data
curl "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd&include_market_cap=true&include_24hr_vol=true"
```

### List Supported Currencies

```bash
curl "https://api.coingecko.com/api/v3/simple/supported_vs_currencies" | jq '.'
```

### Test Rate Limiting

```bash
# Rapid requests to observe rate limiting
for i in {1..50}; do
  curl -w "Status: %{http_code}\n" \
    "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd" \
    -H "x-ratelimit-header-only: true"
  sleep 0.1
done
```

---

## 10. Decision Matrix

| Aspect | Free Tier | Pro Tier | Recommendation |
|--------|-----------|----------|-----------------|
| **Rate Limit** | 10-50 req/min | 50 req/min guaranteed | Start free, upgrade if needed |
| **Uptime SLA** | None | 99.9% | Free for low-traffic, Pro for critical |
| **Cost** | $0 | ~$50-200/month | Cost-benefit at 10K+ daily visitors |
| **Authentication** | None | API key required | Requires environment setup |
| **Caching Impact** | Critical (reduces calls) | Optional (higher limit) | Always implement caching |
| **Fallback Strategy** | Required | Recommended | Always implement graceful degradation |

---

## Conclusion

CoinGecko API is production-ready for the Umbraco Lightning Payments multi-currency feature. The `/simple/price` endpoint provides reliable exchange rate data with straightforward integration. Combined with intelligent caching (5-minute TTL), error handling, and graceful degradation, the solution meets all requirements (FR-019 through FR-022) and supports future growth with the Pro API upgrade path.

**Key Takeaways**:
1. Use free tier with 5-minute cache for most deployments
2. Implement robust error handling with circuit breakers
3. Monitor rate limits and plan Pro tier upgrade path
4. Always provide fallback display (satoshi-only) when rates unavailable
5. Track metrics for cache hit ratio and API response times

