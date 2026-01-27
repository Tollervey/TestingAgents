using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.ExchangeRate;

/// <summary>
/// HTTP client for CoinGecko API v3.
/// Fetches Bitcoin exchange rates with error mapping and logging.
/// </summary>
public class CoinGeckoClient : ICoinGeckoClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoinGeckoClient> _logger;

    public CoinGeckoClient(
        HttpClient httpClient,
        ILogger<CoinGeckoClient> logger,
        IOptions<ExchangeRateOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(opts.CoinGecko.BaseUrl);
        }
        _httpClient.Timeout = TimeSpan.FromSeconds(opts.CoinGecko.TimeoutSeconds);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Lightning-Payments/1.0");

        if (!string.IsNullOrEmpty(opts.CoinGecko.ApiKey))
        {
            var headerName = opts.CoinGecko.UsePro ? "x-cg-pro-api-key" : "x-cg-demo-api-key";
            _httpClient.DefaultRequestHeaders.Add(headerName, opts.CoinGecko.ApiKey);
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, decimal>> GetBitcoinPricesAsync(
        string[] fiatCurrencies,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(fiatCurrencies);
        if (fiatCurrencies.Length == 0)
            throw new ArgumentException("At least one currency is required.", nameof(fiatCurrencies));

        var currenciesParam = string.Join(",", fiatCurrencies.Select(c => c.ToLowerInvariant()));
        var requestUri = $"/api/v3/simple/price?ids=bitcoin&vs_currencies={Uri.EscapeDataString(currenciesParam)}&precision=8";

        _logger.LogDebug("Fetching Bitcoin rates for: {Currencies}", currenciesParam);

        using var response = await _httpClient.GetAsync(requestUri, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "CoinGecko API error {StatusCode}: {Content}",
                response.StatusCode, errorContent);

            response.EnsureSuccessStatusCode(); // Throws HttpRequestException
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        return ParseBitcoinRates(json, fiatCurrencies);
    }

    private static Dictionary<string, decimal> ParseBitcoinRates(string json, string[] requestedCurrencies)
    {
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("bitcoin", out var bitcoinData))
        {
            throw new InvalidOperationException("CoinGecko response missing 'bitcoin' property.");
        }

        foreach (var currency in requestedCurrencies)
        {
            var key = currency.ToLowerInvariant();
            if (bitcoinData.TryGetProperty(key, out var rateElement) && rateElement.TryGetDecimal(out var rate))
            {
                rates[currency.ToUpperInvariant()] = rate;
            }
        }

        return rates;
    }
}
