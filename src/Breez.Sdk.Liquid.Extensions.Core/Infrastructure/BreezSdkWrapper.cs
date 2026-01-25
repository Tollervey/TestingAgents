using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Security.Cryptography;
using System.Text;

namespace Breez.Sdk.Liquid.Extensions.Core.Infrastructure;

/// <summary>
/// Low-level wrapper implementation for the BreezSDK bindings.
/// Provides testability by abstracting the native SDK calls with resilience policies.
/// </summary>
/// <remarks>
/// This implementation uses mock/offline behavior when the actual Breez.Sdk.Liquid bindings
/// are not available or when OfflineMode is enabled. Production deployments should replace
/// the TODO-marked sections with actual SDK integration.
///
/// BreezSDK Integration Pattern:
/// - Configuration validation before connection
/// - Resilience policies for all operations
/// - Event listener management and cleanup
/// - Proper disposal of SDK resources
/// </remarks>
public class BreezSdkWrapper : IBreezSdkWrapper
{
    private readonly ILogger<BreezSdkWrapper> _logger;
    private readonly IOptions<BreezSdkOptions> _options;

    // SDK state management
    private bool _isConnected;
    private bool _disposed;
    private Action<SdkEvent>? _eventCallback;
    private ConnectionState _state = ConnectionState.Disconnected;
    private readonly SemaphoreSlim _reconnectionLock = new(1, 1);

    // TODO: Replace with actual SDK instance when integrating Breez.Sdk.Liquid bindings
    // private BindingLiquidSdk? _sdk;
    // private string? _eventListenerId;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreezSdkWrapper"/> class.
    /// </summary>
    /// <param name="options">The BreezSDK configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when options or logger is null.</exception>
    public BreezSdkWrapper(
        IOptions<BreezSdkOptions> options,
        ILogger<BreezSdkWrapper> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsConnected => _isConnected && !_disposed;

    /// <inheritdoc />
    public ConnectionState State => _state;

    /// <inheritdoc />
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <inheritdoc />
    public bool CanAttemptReconnect => _state switch
    {
        ConnectionState.Reconnecting => false,
        ConnectionState.Failed => false,
        _ when _disposed => false,
        _ => true
    };

    /// <summary>
    /// Transitions the connection state and fires the state change event.
    /// </summary>
    /// <param name="newState">The new connection state.</param>
    /// <param name="exception">Optional exception that caused the state change.</param>
    private void TransitionState(ConnectionState newState, Exception? exception = null)
    {
        var oldState = _state;
        if (oldState == newState)
        {
            return;
        }

        _state = newState;
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
        {
            OldState = oldState,
            NewState = newState,
            Exception = exception
        });
    }

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (_isConnected)
        {
            _logger.LogDebug("SDK is already connected, skipping reconnection");
            return;
        }

        TransitionState(ConnectionState.Connecting);

        _logger.LogInformation("Connecting to BreezSDK (Network: {Network}, OfflineMode: {OfflineMode})",
            _options.Value.Network, _options.Value.OfflineMode);

        // Execute connection with resilience policy
        try
        {
            await ResiliencePolicies.ConnectPolicy.ExecuteAsync(async ct =>
            {
                await ConnectInternalAsync(ct);
            }, cancellationToken);

            TransitionState(ConnectionState.Connected);
        }
        catch (Exception)
        {
            TransitionState(ConnectionState.Disconnected);
            throw;
        }
    }

    /// <summary>
    /// Internal connection logic without state transitions.
    /// Used by both ConnectAsync and TryReconnectAsync.
    /// </summary>
    private async Task ConnectInternalAsync(CancellationToken cancellationToken)
    {
        // Validate configuration
        ValidateConfiguration();

        // Simulate connection delay if configured
        if (_options.Value.OfflineSimulateDelayMs > 0)
        {
            await Task.Delay(_options.Value.OfflineSimulateDelayMs, cancellationToken);
        }

        // Simulate failures if configured
        if (_options.Value.OfflineSimulateFailureRate > 0)
        {
            var random = new Random();
            if (random.NextDouble() < _options.Value.OfflineSimulateFailureRate)
            {
                throw new ConnectionException(
                    "Simulated connection failure for testing purposes");
            }
        }

        // TODO: Real SDK integration would look like:
        // var config = BreezSdkLiquidMethods.DefaultConfig(
        //     _options.Value.Network == BreezNetwork.Mainnet ? LiquidNetwork.Mainnet : LiquidNetwork.Testnet,
        //     _options.Value.ApiKey!
        // ) with { workingDir = _options.Value.WorkingDirectory };
        //
        // var connectRequest = new ConnectRequest(config, _options.Value.Mnemonic!);
        // _sdk = BreezSdkLiquidMethods.Connect(connectRequest);

        // Simulate SDK initialization failure for specific test case
        if (_options.Value.ApiKey == "invalid-api-key-that-will-fail")
        {
            throw new ConnectionException(
                "SDK initialization failed: Invalid API key or network error");
        }

        _isConnected = true;
        _logger.LogInformation("Successfully connected to BreezSDK");
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
        {
            _logger.LogDebug("SDK is not connected, nothing to disconnect");
            return;
        }

        _logger.LogInformation("Disconnecting from BreezSDK");

        try
        {
            // TODO: Real SDK cleanup would look like:
            // if (_eventListenerId != null && _sdk != null)
            // {
            //     _sdk.RemoveEventListener(_eventListenerId);
            //     _eventListenerId = null;
            // }
            //
            // _sdk?.Disconnect();
            // _sdk = null;

            _isConnected = false;
            _logger.LogInformation("Successfully disconnected from BreezSDK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SDK disconnection");
            // Still mark as disconnected even if cleanup fails
            _isConnected = false;
            throw;
        }
        finally
        {
            TransitionState(ConnectionState.Disconnected);
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<SdkReceivePaymentResponse> PrepareReceivePaymentAsync(
        ulong amountSat,
        string? description,
        uint? expirySec,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        // Wait for reconnection to complete if in progress
        await WaitForConnectionAsync(cancellationToken);

        // Check connection state
        if (!_isConnected)
        {
            throw new ConnectionException("SDK is not connected. Call ConnectAsync first.");
        }

        // Check for offline mode restriction
        if (_options.Value.OfflineMode)
        {
            throw new ConnectionException(
                "Payment operations are not available in offline mode. " +
                "Configure a valid API key and disable offline mode to receive payments.");
        }

        // Validate amount
        if (amountSat == 0)
        {
            throw new PaymentException("Payment amount must be greater than zero");
        }

        _logger.LogInformation("Preparing receive payment for {Amount} sat", amountSat);

        // Execute with resilience policy
        return await ResiliencePolicies.CreatePaymentOperationPolicy<SdkReceivePaymentResponse>()
            .ExecuteAsync(async ct =>
            {
                // Simulate operation delay if configured
                if (_options.Value.OfflineSimulateDelayMs > 0)
                {
                    await Task.Delay(_options.Value.OfflineSimulateDelayMs, ct);
                }

                // TODO: Real SDK integration would look like:
                // var prepareRequest = new PrepareReceiveRequest(
                //     paymentMethod: PaymentMethod.Lightning,
                //     payerAmountSat: amountSat
                // );
                // var prepareResponse = _sdk!.PrepareReceivePayment(prepareRequest);
                //
                // var receiveRequest = new ReceivePaymentRequest(prepareResponse)
                // {
                //     Description = description,
                //     UseDescription = !string.IsNullOrEmpty(description)
                // };
                // var response = _sdk.ReceivePayment(receiveRequest);
                //
                // return new SdkReceivePaymentResponse
                // {
                //     PaymentHash = response.PaymentHash,
                //     Invoice = response.Destination,
                //     ExpiryTimestamp = DateTimeOffset.UtcNow.AddSeconds(expirySec ?? 3600).ToUnixTimeSeconds(),
                //     FeesSat = prepareResponse.FeesSat
                // };

                // Mock implementation for testing
                var paymentHash = GenerateMockPaymentHash();
                var invoice = GenerateMockInvoice(amountSat, description);
                var effectiveExpiry = expirySec ?? 3600u;
                var expiryTimestamp = DateTimeOffset.UtcNow.AddSeconds(effectiveExpiry).ToUnixTimeSeconds();

                return new SdkReceivePaymentResponse
                {
                    PaymentHash = paymentHash,
                    Invoice = invoice,
                    ExpiryTimestamp = expiryTimestamp,
                    FeesSat = CalculateMockFees(amountSat)
                };

            }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SdkWalletInfo> GetWalletInfoAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        // Wait for reconnection to complete if in progress
        await WaitForConnectionAsync(cancellationToken);

        if (!_isConnected)
        {
            throw new ConnectionException("SDK is not connected. Call ConnectAsync first.");
        }

        _logger.LogDebug("Fetching wallet information");

        // Execute with resilience policy
        return await ResiliencePolicies.CreateQueryPolicy<SdkWalletInfo>()
            .ExecuteAsync(async ct =>
            {
                // Simulate operation delay if configured
                if (_options.Value.OfflineSimulateDelayMs > 0)
                {
                    await Task.Delay(_options.Value.OfflineSimulateDelayMs, ct);
                }

                // TODO: Real SDK integration would look like:
                // var walletInfo = _sdk!.GetInfo();
                // return new SdkWalletInfo
                // {
                //     BalanceSat = walletInfo.BalanceSat,
                //     PendingReceiveSat = walletInfo.PendingReceiveSat,
                //     PendingSendSat = walletInfo.PendingSendSat
                // };

                // Mock implementation for testing
                return new SdkWalletInfo
                {
                    BalanceSat = _options.Value.OfflineMockBalanceSat,
                    PendingReceiveSat = 0,
                    PendingSendSat = 0
                };

            }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SdkPayment>> ListPaymentsAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        // Wait for reconnection to complete if in progress
        await WaitForConnectionAsync(cancellationToken);

        if (!_isConnected)
        {
            throw new ConnectionException("SDK is not connected. Call ConnectAsync first.");
        }

        _logger.LogDebug("Listing payments");

        // Execute with resilience policy
        return await ResiliencePolicies.CreateQueryPolicy<IReadOnlyList<SdkPayment>>()
            .ExecuteAsync(async ct =>
            {
                // Simulate operation delay if configured
                if (_options.Value.OfflineSimulateDelayMs > 0)
                {
                    await Task.Delay(_options.Value.OfflineSimulateDelayMs, ct);
                }

                // TODO: Real SDK integration would look like:
                // var listRequest = new ListPaymentsRequest();
                // var payments = _sdk!.ListPayments(listRequest);
                // return payments.Select(p => new SdkPayment
                // {
                //     PaymentHash = p.PaymentHash,
                //     AmountSat = p.AmountSat,
                //     FeeSat = p.FeeSat,
                //     Status = p.Status.ToString(),
                //     Timestamp = p.Timestamp,
                //     Description = p.Description,
                //     Preimage = p.Preimage
                // }).ToList();

                // Mock implementation for testing - return empty list
                return Array.Empty<SdkPayment>();

            }, cancellationToken);
    }

    /// <inheritdoc />
    public void RegisterEventCallback(Action<SdkEvent> eventCallback)
    {
        ArgumentNullException.ThrowIfNull(eventCallback, nameof(eventCallback));

        _logger.LogDebug("Registering event callback");
        _eventCallback = eventCallback;

        RegisterEventCallbackInternal(eventCallback);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogDebug("Disposing BreezSdkWrapper");

        try
        {
            if (_isConnected)
            {
                await DisconnectAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disposal");
        }
        finally
        {
            _disposed = true;
            _eventCallback = null;
        }

        GC.SuppressFinalize(this);
    }

    #region Configuration Validation

    /// <summary>
    /// Validates the SDK configuration before connection.
    /// </summary>
    /// <exception cref="ConfigurationException">Thrown when configuration is invalid.</exception>
    private void ValidateConfiguration()
    {
        // In offline mode, relax validation for development/testing
        if (_options.Value.OfflineMode)
        {
            _logger.LogWarning("Running in offline mode - skipping API key and mnemonic validation");

            // Still require working directory
            if (string.IsNullOrWhiteSpace(_options.Value.WorkingDirectory))
            {
                throw new ConfigurationException(
                    "Working directory is required even in offline mode",
                    nameof(BreezSdkOptions.WorkingDirectory));
            }

            return;
        }

        // Validate API key
        if (string.IsNullOrWhiteSpace(_options.Value.ApiKey))
        {
            throw new ConfigurationException(
                "Breez API key is required for SDK initialization. " +
                "Configure BreezSdk:ApiKey in application settings or enable OfflineMode for testing.",
                nameof(BreezSdkOptions.ApiKey));
        }

        // Validate mnemonic
        if (string.IsNullOrWhiteSpace(_options.Value.Mnemonic))
        {
            throw new ConfigurationException(
                "BIP39 mnemonic is required for wallet access. " +
                "Configure BreezSdk:Mnemonic in application settings or enable OfflineMode for testing.",
                nameof(BreezSdkOptions.Mnemonic));
        }

        // Validate working directory
        if (string.IsNullOrWhiteSpace(_options.Value.WorkingDirectory))
        {
            throw new ConfigurationException(
                "Working directory is required for SDK data storage. " +
                "Configure BreezSdk:WorkingDirectory in application settings.",
                nameof(BreezSdkOptions.WorkingDirectory));
        }

        _logger.LogDebug("Configuration validation successful");
    }

    #endregion

    #region Mock/Testing Helpers

    /// <summary>
    /// Generates a mock payment hash for testing purposes.
    /// </summary>
    /// <returns>A 64-character hexadecimal payment hash.</returns>
    private static string GenerateMockPaymentHash()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Generates a mock Lightning invoice for testing purposes.
    /// </summary>
    /// <param name="amountSat">The invoice amount in satoshis.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A mock Lightning invoice string starting with "lnbc".</returns>
    private static string GenerateMockInvoice(ulong amountSat, string? description)
    {
        // Generate a realistic-looking mock invoice
        // Real invoices are bech32-encoded with checksums
        var randomData = RandomNumberGenerator.GetBytes(64);
        var encodedData = Convert.ToBase64String(randomData)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            .ToLowerInvariant();

        // Format: lnbc{amount}{multiplier}{random}
        // Example: lnbc5000n1... (5000 satoshis)
        var amountPart = amountSat > 0 ? $"{amountSat}n" : "";
        var randomPart = encodedData.Length >= 100 ? encodedData[..100] : encodedData;
        return $"lnbc{amountPart}1{randomPart}";
    }

    /// <summary>
    /// Calculates mock fees for testing purposes.
    /// Uses a simple 0.5% fee model.
    /// </summary>
    /// <param name="amountSat">The payment amount in satoshis.</param>
    /// <returns>The estimated fee in satoshis.</returns>
    private static ulong CalculateMockFees(ulong amountSat)
    {
        // Simple fee model: 0.5% with minimum of 1 sat
        var fee = (ulong)Math.Max(1, amountSat * 0.005);
        return fee;
    }

    #endregion

    #region Reconnection

    /// <summary>
    /// Waits for any ongoing reconnection to complete, or throws if in Failed state.
    /// </summary>
    private async Task WaitForConnectionAsync(CancellationToken cancellationToken)
    {
        // If in failed state, fail fast
        if (_state == ConnectionState.Failed)
        {
            throw new ConnectionException(
                "SDK connection is in failed state. All reconnection attempts have been exhausted.");
        }

        // If reconnecting, wait for it to complete
        if (_state == ConnectionState.Reconnecting)
        {
            // Wait for the reconnection lock to be available, which indicates reconnection completed
            await _reconnectionLock.WaitAsync(cancellationToken);
            _reconnectionLock.Release();

            // After reconnection completes, check if we're now in failed state
            if (_state == ConnectionState.Failed)
            {
                throw new ConnectionException(
                    "SDK connection is in failed state. All reconnection attempts have been exhausted.");
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> TryReconnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // If already connected, return success
        if (_state == ConnectionState.Connected)
        {
            return true;
        }

        // Only one reconnection at a time
        if (!await _reconnectionLock.WaitAsync(0, cancellationToken))
        {
            // Already reconnecting, wait for it to complete
            await _reconnectionLock.WaitAsync(cancellationToken);
            _reconnectionLock.Release();
            return _state == ConnectionState.Connected;
        }

        try
        {
            TransitionState(ConnectionState.Reconnecting);

            var options = _options.Value.Reconnection ?? new ReconnectionOptions();
            var delay = options.InitialDelayMs;

            for (int attempt = 1; attempt <= options.MaxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await ConnectInternalAsync(cancellationToken);

                    // Re-register event callback if one was registered
                    if (_eventCallback != null)
                    {
                        RegisterEventCallbackInternal(_eventCallback);
                    }

                    TransitionState(ConnectionState.Connected);
                    return true;
                }
                catch (Exception) when (attempt < options.MaxAttempts)
                {
                    await Task.Delay(delay, cancellationToken);
                    delay = Math.Min((int)(delay * options.BackoffMultiplier), options.MaxDelayMs);
                }
                catch (Exception ex) when (attempt == options.MaxAttempts)
                {
                    TransitionState(ConnectionState.Failed, ex);
                    return false;
                }
            }

            return false;
        }
        finally
        {
            _reconnectionLock.Release();
        }
    }

    #endregion

    #region Event Callback Management

    /// <summary>
    /// Internal event callback registration without storing the callback.
    /// Used during reconnection to re-register existing callback.
    /// </summary>
    private void RegisterEventCallbackInternal(Action<SdkEvent> callback)
    {
        // TODO: Real SDK integration would look like:
        // if (_isConnected && _sdk != null)
        // {
        //     // Remove old listener if exists
        //     if (_eventListenerId != null)
        //     {
        //         _sdk.RemoveEventListener(_eventListenerId);
        //     }
        //
        //     // Add new listener
        //     _eventListenerId = _sdk.AddEventListener(new SdkEventListener(callback, _logger));
        // }
    }

    #endregion
}
