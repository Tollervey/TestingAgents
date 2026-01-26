using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;

namespace Breez.Sdk.Liquid.Extensions.Umbraco.Components;

/// <summary>
/// Umbraco Component for managing BreezSDK lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// This component handles initialization and cleanup of the BreezSDK connection
/// during the Umbraco application lifecycle.
/// </para>
/// <para>
/// On startup, the component connects to the BreezSDK (unless in offline mode).
/// On shutdown, it ensures a graceful disconnect.
/// </para>
/// </remarks>
public class BreezSdkComponent : IAsyncComponent
{
    private readonly IBreezSdkService _sdkService;
    private readonly IOptions<BreezSdkOptions> _options;
    private readonly ILogger<BreezSdkComponent> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreezSdkComponent"/> class.
    /// </summary>
    /// <param name="sdkService">The BreezSDK service.</param>
    /// <param name="options">The BreezSDK options.</param>
    /// <param name="logger">The logger instance.</param>
    public BreezSdkComponent(
        IBreezSdkService sdkService,
        IOptions<BreezSdkOptions> options,
        ILogger<BreezSdkComponent> logger)
    {
        _sdkService = sdkService ?? throw new ArgumentNullException(nameof(sdkService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the BreezSDK connection when Umbraco starts.
    /// </summary>
    /// <param name="isRestarting">Whether Umbraco is restarting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InitializeAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        if (_options.Value.OfflineMode)
        {
            _logger.LogInformation("BreezSDK running in offline mode - skipping connection");
            return;
        }

        try
        {
            _logger.LogInformation(
                "Initializing BreezSDK connection for network {Network}",
                _options.Value.Network);

            await _sdkService.ConnectAsync(cancellationToken);

            _logger.LogInformation("BreezSDK connected successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to initialize BreezSDK connection. The service will retry automatically.");

            // Don't throw - let the application start even if SDK connection fails
            // The service will handle reconnection attempts
        }
    }

    /// <summary>
    /// Disconnects from BreezSDK when Umbraco shuts down.
    /// </summary>
    /// <param name="isRestarting">Whether Umbraco is restarting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task TerminateAsync(bool isRestarting, CancellationToken cancellationToken)
    {
        if (_options.Value.OfflineMode)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Disconnecting from BreezSDK");
            await _sdkService.DisconnectAsync(cancellationToken);
            _logger.LogInformation("BreezSDK disconnected successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during BreezSDK disconnect");
        }
    }
}
