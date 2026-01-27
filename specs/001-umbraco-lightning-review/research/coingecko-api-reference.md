# CoinGecko API Reference & Testing Guide

**Document ID**: CoinGecko-Reference-001
**Date**: 2026-01-26
**Status**: Reference Complete

## Table of Contents

1. [API Response Examples](#1-api-response-examples)
2. [HTTP Status Codes & Error Responses](#2-http-status-codes--error-responses)
3. [Rate Limiting Details](#3-rate-limiting-details)
4. [Test Utilities & Mocks](#4-test-utilities--mocks)
5. [cURL Command Reference](#5-curl-command-reference)
6. [Troubleshooting Guide](#6-troubleshooting-guide)

---

## 1. API Response Examples

### Success Response: Simple Price

**Request**:
```
GET /api/v3/simple/price?ids=bitcoin&vs_currencies=usd,eur,gbp,jpy&include_last_updated_at=true&precision=2
Host: api.coingecko.com
Accept: application/json
```

**Response (200 OK)**:
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

**Response Structure Notes**:
- Root key = cryptocurrency ID (always `bitcoin` for our use case)
- Currency codes (ISO 4217) are object keys
- Values are decimal numbers representing rates
- `last_updated_at` is optional, Unix timestamp in seconds
- No wrapping metadata object

### Success Response: Single Currency

**Request**:
```
GET /api/v3/simple/price?ids=bitcoin&vs_currencies=usd&precision=8
```

**Response (200 OK)**:
```json
{
  "bitcoin": {
    "usd": 42500.50000000
  }
}
```

### Success Response: With Market Data

**Request**:
```
GET /api/v3/simple/price?ids=bitcoin&vs_currencies=usd&include_market_cap=true&include_24hr_vol=true&precision=2
```

**Response (200 OK)**:
```json
{
  "bitcoin": {
    "usd": 42500.50,
    "usd_market_cap": 831455000000,
    "usd_24h_vol": 28500000000
  }
}
```

### Success Response: Supported Currencies

**Request**:
```
GET /api/v3/simple/supported_vs_currencies
```

**Response (200 OK)**:
```json
[
  "btc",
  "eth",
  "ltc",
  "bch",
  "bnb",
  "eos",
  "xrp",
  "xlm",
  "link",
  "dot",
  "yfi",
  "aed",
  "ars",
  "aud",
  "bdt",
  "bhd",
  "bmd",
  "brl",
  "cad",
  "chf",
  "clp",
  "cny",
  "czk",
  "dkk",
  "eur",
  "gbp",
  "eur",
  "hkd",
  "huf",
  "idr",
  "ils",
  "inr",
  "jpy",
  "krw",
  "kwd",
  "lkr",
  "mmk",
  "mxn",
  "myr",
  "ngn",
  "nok",
  "nzd",
  "php",
  "pkr",
  "pln",
  "rub",
  "sek",
  "sgd",
  "thb",
  "try",
  "twd",
  "uah",
  "usd",
  "vef",
  "vnd",
  "yfi",
  "zar",
  "xlm",
  "xrp",
  "yfi",
  "zcn"
]
```

---

## 2. HTTP Status Codes & Error Responses

### 200 OK (Success)

All valid requests with matching data return 200.

### 400 Bad Request

**Scenario**: Invalid query parameter

```json
{
  "status": {
    "error_code": 1006,
    "error_message": "invalid vs_currencies"
  }
}
```

**Common Causes**:
- Typo in currency code (e.g., `usd1` instead of `usd`)
- Invalid cryptocurrency ID (e.g., `bitcoin_cash` instead of `bitcoin`)
- Malformed parameters

**Example Curl**:
```bash
curl "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=invalid_currency"
# Response: 400 Bad Request
```

### 401 Unauthorized (Pro API Only)

**Scenario**: Invalid or missing Pro API key

```json
{
  "status": {
    "error_code": 1001,
    "error_message": "You call the API with the invalid API key."
  }
}
```

**Headers**:
```
HTTP/1.1 401 Unauthorized
x-ratelimit-limit-minute: 50
x-ratelimit-remaining-minute: 0
```

### 429 Too Many Requests

**Scenario**: Rate limit exceeded

```json
{
  "status": {
    "error_code": 1002,
    "error_message": "You've reached the Rate limit. Please visit https://www.coingecko.com/en/api/pricing to subscribe to a plan."
  }
}
```

**Headers**:
```
HTTP/1.1 429 Too Many Requests
x-ratelimit-limit-minute: 10
x-ratelimit-remaining-minute: 0
Retry-After: 61
Content-Type: application/json
```

**Strategy**: Extract `Retry-After` header and wait before retrying

```csharp
if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
{
    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
    await Task.Delay(retryAfter);
    // Retry request
}
```

### 500 Internal Server Error

**Scenario**: CoinGecko API issue

```json
{
  "status": {
    "error_code": 1000,
    "error_message": "Internal server error"
  }
}
```

**Strategy**: Treat as transient, retry with exponential backoff

### 503 Service Unavailable

**Scenario**: CoinGecko maintenance or overload

```json
{
  "status": {
    "error_code": 1003,
    "error_message": "Service is temporarily unavailable."
  }
}
```

**Strategy**: Same as 500, retry with exponential backoff

---

## 3. Rate Limiting Details

### Free Tier Rate Limiting

**Limit**: 10-50 requests per minute (non-guaranteed)

**Headers Present**:
```
x-ratelimit-limit-minute: 10
x-ratelimit-remaining-minute: 9
```

**Behavior**:
- Requests after limit are dropped (429)
- No guaranteed rate, may vary based on server load
- Multiple IPs from same organization may share quota

### Pro Tier Rate Limiting

**Limit**: 50 requests per minute (guaranteed)

**Headers Present**:
```
x-ratelimit-limit-minute: 50
x-ratelimit-remaining-minute: 49
```

**Behavior**:
- Guaranteed limit per API key
- 1 request per 1.2 seconds
- SLA-backed (99.9% uptime)

### Rate Limit Monitoring Pattern

```csharp
private void MonitorRateLimit(HttpResponseMessage response)
{
    if (response.Headers.TryGetValues("x-ratelimit-limit-minute", out var limitValues) &&
        response.Headers.TryGetValues("x-ratelimit-remaining-minute", out var remainingValues))
    {
        if (int.TryParse(limitValues.FirstOrDefault(), out var limit) &&
            int.TryParse(remainingValues.FirstOrDefault(), out var remaining))
        {
            var usagePercent = ((double)(limit - remaining) / limit) * 100;

            _logger.LogInformation(
                "CoinGecko API usage: {UsagePercent}% ({Remaining}/{Limit})",
                usagePercent, remaining, limit);

            if (remaining < (limit * 0.1))
            {
                _logger.LogWarning(
                    "CoinGecko rate limit pressure: {Remaining} requests remaining",
                    remaining);

                // Alert monitoring system
                _metrics.RecordRateLimitPressure(remaining);
            }
        }
    }
}
```

---

## 4. Test Utilities & Mocks

### HttpClientBuilder for Testing

```csharp
namespace Umbraco.Community.LightningPayments.Tests.Infrastructure.Builders;

/// <summary>
/// Builder for configuring mocked HTTP clients in tests
/// </summary>
public sealed class MockHttpClientBuilder
{
    private readonly HttpResponseMessage _responseMessage;

    public MockHttpClientBuilder()
    {
        _responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
    }

    public MockHttpClientBuilder WithStatusCode(System.Net.HttpStatusCode statusCode)
    {
        _responseMessage.StatusCode = statusCode;
        return this;
    }

    public MockHttpClientBuilder WithJsonContent(object content)
    {
        var json = JsonSerializer.Serialize(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        _responseMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return this;
    }

    public MockHttpClientBuilder WithRateLimitHeaders(int limit, int remaining)
    {
        _responseMessage.Headers.Add("x-ratelimit-limit-minute", limit.ToString());
        _responseMessage.Headers.Add("x-ratelimit-remaining-minute", remaining.ToString());
        return this;
    }

    public MockHttpClientBuilder WithRetryAfter(int seconds)
    {
        _responseMessage.Headers.Add("Retry-After", seconds.ToString());
        return this;
    }

    public HttpClient Build()
    {
        var handler = new MockHttpMessageHandler(_responseMessage);
        return new HttpClient(handler);
    }
}

public sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _responseMessage;
    public List<HttpRequestMessage> Requests { get; } = new();

    public MockHttpMessageHandler(HttpResponseMessage responseMessage)
    {
        _responseMessage = responseMessage;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        // Create a clone so response can be reused
        var clone = new HttpResponseMessage(_responseMessage.StatusCode);
        foreach (var header in _responseMessage.Headers)
        {
            clone.Headers.Add(header.Key, header.Value);
        }

        clone.Content = new StringContent(
            _responseMessage.Content?.ReadAsStringAsync(cancellationToken).Result ?? "{}",
            Encoding.UTF8,
            "application/json");

        return Task.FromResult(clone);
    }
}
```

### Test Data Builder

```csharp
namespace Umbraco.Community.LightningPayments.Tests.Infrastructure.Builders;

/// <summary>
/// Builder for creating CoinGecko API response fixtures
/// </summary>
public sealed class CoinGeckoResponseBuilder
{
    private Dictionary<string, decimal> _rates = new();
    private long _lastUpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    private bool _includeMarketCap = false;

    public CoinGeckoResponseBuilder WithRate(string currency, decimal ratePerBtc)
    {
        _rates[currency.ToLowerInvariant()] = ratePerBtc;
        return this;
    }

    public CoinGeckoResponseBuilder WithRates(params (string currency, decimal rate)[] rates)
    {
        foreach (var (currency, rate) in rates)
        {
            _rates[currency.ToLowerInvariant()] = rate;
        }

        return this;
    }

    public CoinGeckoResponseBuilder WithLastUpdatedAt(DateTimeOffset timestamp)
    {
        _lastUpdatedAt = timestamp.ToUnixTimeSeconds();
        return this;
    }

    public CoinGeckoResponseBuilder WithMarketCap(bool include = true)
    {
        _includeMarketCap = include;
        return this;
    }

    public Dictionary<string, object> Build()
    {
        var bitcoinData = new Dictionary<string, object>(_rates.Cast<string, object>());
        bitcoinData["last_updated_at"] = _lastUpdatedAt;

        if (_includeMarketCap)
        {
            bitcoinData["usd_market_cap"] = 831455000000;
        }

        return new Dictionary<string, object>
        {
            { "bitcoin", bitcoinData }
        };
    }

    public string BuildJson() =>
        JsonSerializer.Serialize(Build(), new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
}
```

### Example Test Using Builders

```csharp
[Theory]
[InlineData("USD")]
[InlineData("EUR")]
[InlineData("GBP")]
public async Task GetBitcoinRatesAsync_WithValidCurrency_ReturnsParsedRate(string currency)
{
    // Arrange
    var responseData = new CoinGeckoResponseBuilder()
        .WithRate(currency, 42500.50m)
        .Build();

    var httpClient = new MockHttpClientBuilder()
        .WithJsonContent(responseData)
        .WithRateLimitHeaders(limit: 10, remaining: 9)
        .Build();

    var httpClientMock = new Mock<ICoinGeckoHttpClient>();
    httpClientMock
        .Setup(x => x.GetSimplePriceAsync(
            It.Is<IEnumerable<string>>(c => c.Contains(currency)),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new Dictionary<string, decimal> { { currency, 42500.50m } });

    var service = new CoinGeckoExchangeRateService(
        httpClientMock.Object,
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
        new NullLogger<CoinGeckoExchangeRateService>(),
        Options.Create(new CoinGeckoOptions()));

    // Act
    var result = await service.GetBitcoinRatesAsync(new[] { currency });

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Single(result.Value!.Rates);
    Assert.Equal(42500.50m, result.Value.Rates[currency].RatePerBitcoin);
}
```

---

## 5. cURL Command Reference

### Fetch Bitcoin Price in USD

```bash
curl "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd"
```

### Fetch Multiple Currencies

```bash
curl "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd,eur,gbp,jpy,aud,cad,chf,cny,inr,krw,zar"
```

### With Pro API Key

```bash
curl "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd" \
  -H "x-cg-pro-api-key: YOUR_API_KEY_HERE"
```

### With Market Data

```bash
curl "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd,eur&include_market_cap=true&include_24hr_vol=true&include_last_updated_at=true"
```

### List Supported Currencies

```bash
curl "https://api.coingecko.com/api/v3/simple/supported_vs_currencies" | jq '.' | sort
```

### Pretty Print JSON Response

```bash
curl -s "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd,eur" | jq '.'
```

### Save Response to File

```bash
curl "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd" \
  -o response.json
```

### Test Rate Limiting (Free Tier)

```bash
# Rapid requests to observe rate limiting
for i in {1..50}; do
  echo "Request $i:"
  curl -i -s "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd" \
    -w "\nRate limit remaining: %{header{x-ratelimit-remaining-minute}}\n\n" \
    2>/dev/null | grep -E "^HTTP|Rate limit"
  sleep 1
done
```

### Check Response Headers Only

```bash
curl -I "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd"
```

### Measure Response Time

```bash
curl -w "\nResponse time: %{time_total}s\n" \
  "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd"
```

### Test with Different Precisions

```bash
# 2 decimal places (typical for fiat)
curl "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd&precision=2"

# 8 decimal places (for crypto pairs)
curl "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=btc&precision=8"
```

---

## 6. Troubleshooting Guide

### Problem: "You've reached the Rate limit"

**Symptoms**: Receiving 429 status code with error message

**Root Cause**: Exceeding 10-50 requests/minute (free tier)

**Solutions**:
1. **Immediate**: Implement caching (5-minute TTL minimum)
2. **Short-term**: Batch requests (e.g., fetch multiple currencies in one call)
3. **Long-term**: Upgrade to Pro tier if >10K daily visitors

**Diagnostic**:
```bash
# Check rate limit remaining
for i in {1..5}; do
  curl -I "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd" 2>/dev/null | grep x-ratelimit
  sleep 1
done
```

### Problem: Empty Response or Null Rates

**Symptoms**: Response 200 OK but missing expected currencies

**Root Cause**: Typo in currency code or API response format mismatch

**Solutions**:
1. Verify currency codes match [supported list](#success-response-supported-currencies)
2. Check request parameters in logs
3. Compare with cURL test

**Diagnostic**:
```bash
# Verify specific currency is supported
curl "https://api.coingecko.com/api/v3/simple/supported_vs_currencies" | jq '.' | grep -i "usd"

# Test the problematic currency
curl "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd" | jq '.'
```

### Problem: Timeout / Slow Response

**Symptoms**: Request takes >10 seconds or times out

**Root Cause**:
- Network latency to CoinGecko
- CoinGecko server overload
- Client timeout too aggressive

**Solutions**:
1. Increase timeout to 15-20 seconds for development
2. Verify network connectivity: `ping api.coingecko.com`
3. Test from different network (WiFi vs mobile)
4. Check CoinGecko status page: https://status.coingecko.com/

**Diagnostic**:
```bash
# Measure response time
curl -w "\n%{time_total}\n" \
  "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd"

# Test DNS resolution
nslookup api.coingecko.com

# Check network path
tracert api.coingecko.com
```

### Problem: Pro API Key Not Working

**Symptoms**: 401 Unauthorized with Pro API key configured

**Root Cause**:
- API key not configured correctly
- API key expired or invalid
- Header name misspelled

**Solutions**:
1. Verify header name is exactly `x-cg-pro-api-key` (case-sensitive)
2. Verify API key is not expired in CoinGecko dashboard
3. Test with cURL first

**Diagnostic**:
```bash
# Test Pro API key with cURL
curl -H "x-cg-pro-api-key: YOUR_KEY" \
  "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd"

# Check if key is in environment correctly
echo $COINGECKO_PRO_API_KEY  # Should print key, not empty
```

### Problem: Cache Not Being Used

**Symptoms**: Every request hits the API (rate limit pressure)

**Root Cause**:
- Cache key generation mismatch
- Cache invalidation too aggressive
- Distributed cache not persisting

**Solutions**:
1. Add logging to cache get/set operations
2. Verify cache key is consistent
3. For distributed cache, check Redis/SQL Server connectivity

**Diagnostic**:
```csharp
// Add to service for debugging
_logger.LogInformation("Cache key: {Key}", cacheKey);
_logger.LogInformation("Cache get result: {HasValue}", await _cache.GetStringAsync(cacheKey) != null);
```

### Problem: Inconsistent Exchange Rates

**Symptoms**: Same currency returns different rates in quick succession

**Root Cause**:
- Fetching from API without cache (getting live rates)
- Cache TTL too short (refreshing too frequently)
- Stale cache fallback returning old rates

**Solutions**:
1. Verify cache TTL is 5 minutes minimum
2. Check if fresh fetch is intentional (vs cache)
3. Log rate values and cache status

**Verification**:
```csharp
var result = await _exchangeRateService.GetBitcoinRatesAsync(new[] { "USD" });
_logger.LogInformation(
    "Rate: {Rate}, From cache: {IsFromCache}, Fetched: {FetchedAt}",
    result.Value.Rates["USD"].RatePerBitcoin,
    result.Value.IsFromCache,
    result.Value.FetchedAt);
```

### Problem: Missing CoinGecko Service in Tests

**Symptoms**: `IExchangeRateService` not registered in test container

**Root Cause**: Forgot to call `AddCoinGeckoExchangeRates()` in test setup

**Solution**:
```csharp
[Fact]
public async Task MyTest()
{
    var services = new ServiceCollection();

    // Add required services
    services.AddLogging();
    services.AddOptions();
    services.AddDistributedMemoryCache();
    services.AddCoinGeckoExchangeRates(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            { "CoinGecko:BaseUrl", "https://api.coingecko.com/api/v3" }
        })
        .Build());

    var provider = services.BuildServiceProvider();
    var service = provider.GetRequiredService<IExchangeRateService>();

    // Test...
}
```

---

## Appendix: Currency Code Reference

### Major Fiat Currencies

| Code | Currency | Notes |
|------|----------|-------|
| USD | US Dollar | Most common, recommended default |
| EUR | Euro | Used across eurozone |
| GBP | British Pound | UK, Gibraltar, Isle of Man |
| JPY | Japanese Yen | No decimal places (integer only) |
| AUD | Australian Dollar | Common in Asia-Pacific region |
| CAD | Canadian Dollar | North America |
| CHF | Swiss Franc | Stable, used for trading pairs |
| CNY | Chinese Yuan | Most used in Asia |
| INR | Indian Rupee | Large Asian market |
| KRW | Korean Won | No decimal places (integer only) |
| ZAR | South African Rand | Emerging market |

### Cryptocurrencies Supported

| Code | Name |
|------|------|
| bitcoin | Bitcoin (BTC) |
| ethereum | Ethereum (ETH) |
| binancecoin | Binance Coin (BNB) |
| litecoin | Litecoin (LTC) |
| ripple | Ripple (XRP) |

---

## Conclusion

This reference guide provides complete information for integrating and troubleshooting CoinGecko API. Use the cURL examples for quick testing and the test builders for unit tests.

