using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public.Dto;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.ExchangeRate;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public;

/// <summary>
/// Public API controller for exchange rate lookup and satoshi-to-fiat conversion.
/// Implements exchange rate endpoints from public-api.yaml specification.
/// </summary>
[ApiController]
[RequireHttps]
[AllowAnonymous]
[Route("api/public/lightning")]
[Produces("application/json")]
public class ExchangeRateController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<ExchangeRateController> _logger;
    private readonly ExchangeRateOptions _options;

    public ExchangeRateController(
        IExchangeRateService exchangeRateService,
        ILogger<ExchangeRateController> logger,
        IOptions<ExchangeRateOptions> options)
    {
        _exchangeRateService = exchangeRateService ?? throw new ArgumentNullException(nameof(exchangeRateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets current Bitcoin exchange rates for configured fiat currencies.
    /// Rates are cached for up to 5 minutes.
    /// </summary>
    /// <param name="currencies">Comma-separated currency codes (default: USD,EUR,GBP).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Exchange rates response.</returns>
    [HttpGet("exchange-rates")]
    [ProducesResponseType(typeof(ExchangeRatesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetExchangeRates(
        [FromQuery] string? currencies = null,
        CancellationToken ct = default)
    {
        try
        {
            var currencyList = ParseCurrencies(currencies);
            var rates = await _exchangeRateService.GetExchangeRatesAsync(currencyList, ct);

            return Ok(new ExchangeRatesResponse
            {
                Rates = rates,
                FetchedAt = DateTimeOffset.UtcNow,
                Source = "CoinGecko",
                IsStale = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch exchange rates");
            return Problem(
                title: "Service Unavailable",
                detail: "Exchange rate service is currently unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    /// <summary>
    /// Converts a satoshi amount to fiat currency.
    /// </summary>
    /// <param name="sats">Amount in satoshis.</param>
    /// <param name="currency">Target ISO 4217 currency code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Conversion result.</returns>
    [HttpGet("convert")]
    [ProducesResponseType(typeof(ConversionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ConvertAmount(
        [FromQuery] long sats,
        [FromQuery] string currency,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length > 3)
        {
            return Problem(
                title: "Invalid Request",
                detail: "A valid ISO 4217 currency code is required (max 3 characters).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var normalizedCurrency = currency.ToUpperInvariant();
            var rates = await _exchangeRateService.GetExchangeRatesAsync([normalizedCurrency], ct);

            if (!rates.TryGetValue(normalizedCurrency, out var ratePerBtc))
            {
                return Problem(
                    title: "Service Unavailable",
                    detail: $"Exchange rate for {normalizedCurrency} is not available.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var ratePerSat = ratePerBtc / 100_000_000m;
            var fiatAmount = Math.Round(sats * ratePerSat, 2);
            var formatted = FormatAmount(fiatAmount, normalizedCurrency);

            return Ok(new ConversionResponse
            {
                Sats = sats,
                Currency = normalizedCurrency,
                FiatAmount = fiatAmount,
                FormattedAmount = formatted,
                RatePerBtc = ratePerBtc,
                IsStale = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert {Sats} sats to {Currency}", sats, currency);
            return Problem(
                title: "Service Unavailable",
                detail: "Exchange rate service is currently unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private string[] ParseCurrencies(string? currencies)
    {
        if (string.IsNullOrWhiteSpace(currencies))
            return _options.SupportedCurrencies;

        return currencies
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(c => c.ToUpperInvariant())
            .Distinct()
            .ToArray();
    }

    private static string FormatAmount(decimal amount, string currency)
    {
        return currency switch
        {
            "USD" => $"${amount:F2}",
            "EUR" => $"€{amount:F2}",
            "GBP" => $"£{amount:F2}",
            "JPY" => $"¥{amount:F0}",
            _ => $"{amount:F2} {currency}"
        };
    }
}
