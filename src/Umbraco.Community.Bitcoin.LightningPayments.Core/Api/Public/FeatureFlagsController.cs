using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public.Dto;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Runtime;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public;

/// <summary>
/// Public API controller for querying feature flag state.
/// Enables frontend applications to determine which features are available.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/lightning/features")]
[Produces("application/json")]
public class FeatureFlagsController : ControllerBase
{
    private readonly IRuntimeSettingsService _runtimeSettings;
    private readonly ILogger<FeatureFlagsController> _logger;
    private readonly NotificationOptions _notificationOptions;
    private readonly ExchangeRateOptions _exchangeRateOptions;

    public FeatureFlagsController(
        IRuntimeSettingsService runtimeSettings,
        ILogger<FeatureFlagsController> logger,
        IOptions<NotificationOptions> notificationOptions,
        IOptions<ExchangeRateOptions> exchangeRateOptions)
    {
        _runtimeSettings = runtimeSettings;
        _logger = logger;
        _notificationOptions = notificationOptions.Value;
        _exchangeRateOptions = exchangeRateOptions.Value;
    }

    /// <summary>
    /// Returns the current feature flag state.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(FeatureFlagsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeatureFlags(CancellationToken cancellationToken)
    {
        var flags = await _runtimeSettings.GetAsync(cancellationToken);

        var response = new FeatureFlagsResponse
        {
            Enabled = flags.Enabled,
            PaywallEnabled = flags.PaywallEnabled,
            TipJarEnabled = flags.TipJarEnabled,
            Bolt12Enabled = flags.Enabled,
            NotificationsEnabled = flags.Enabled && _notificationOptions.Enabled,
            ExchangeRatesEnabled = flags.Enabled && _exchangeRateOptions.Enabled
        };

        return Ok(response);
    }
}
