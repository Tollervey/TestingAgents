using Microsoft.Extensions.Logging;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez;
using Umbraco.Cms.Core.Composing;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Components;

public class BreezSdkComponent : IAsyncComponent
{
    private readonly IBreezSdkService _breezSdkService;
    private readonly ILogger<BreezSdkComponent> _logger;

    public BreezSdkComponent(IBreezSdkService breezSdkService, ILogger<BreezSdkComponent> logger)
    {
        _breezSdkService = breezSdkService;
        _logger = logger;
    }

    public async Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        try
        {
            var connected = await _breezSdkService.IsConnectedAsync();
            _logger.LogInformation("Breez SDK initial connection attempt result: {Connected}", connected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Breez SDK failed to initialize during application startup.");
        }
    }

    public async Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        try
        {
            // Ensure graceful shutdown of the SDK
            await _breezSdkService.DisposeAsync();
            _logger.LogInformation("Breez SDK disposed during application shutdown.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disposing Breez SDK on shutdown.");
        }
    }
}
