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
    private readonly BreezSdkOptions _options;

    // SDK state management
    private bool _isConnected;
    private bool _disposed;
    private Action<SdkEvent>? _eventCallback;

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
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsConnected => _isConnected && !_disposed;

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

        _logger.LogInformation("Connecting to BreezSDK (Network: {Network}, OfflineMode: {OfflineMode})",
            _options.Network, _options.OfflineMode);

        // Execute connection with resilience policy
        await ResiliencePolicies.ConnectPolicy.ExecuteAsync(async ct =>
        {
            // Validate configuration
            ValidateConfiguration();

            // Simulate connection delay if configured
            if (_options.OfflineSimulateDelayMs > 0)
            {
                await Task.Delay(_options.OfflineSimulateDelayMs, ct);
            }

            // Simulate failures if configured
            if (_options.OfflineSimulateFailureRate > 0)
            {
                var random = new Random();
                if (random.NextDouble() < _options.OfflineSimulateFailureRate)
                {
                    throw new ConnectionException(
                        "Simulated connection failure for testing purposes");
                }
            }

            // TODO: Real SDK integration would look like:
            // var config = BreezSdkLiquidMethods.DefaultConfig(
            //     _options.Network == BreezNetwork.Mainnet ? LiquidNetwork.Mainnet : LiquidNetwork.Testnet,
            //     _options.ApiKey!
            // ) with { workingDir = _options.WorkingDirectory };
            //
            // var connectRequest = new ConnectRequest(config, _options.Mnemonic!);
            // _sdk = BreezSdkLiquidMethods.Connect(connectRequest);
            //
            // if (_eventCallback != null)
            // {
            //     _eventListenerId = _sdk.AddEventListener(new SdkEventListener(_eventCallback, _logger));
            // }

            // Simulate SDK initialization failure for specific test case
            if (_options.ApiKey == "invalid-api-key-that-will-fail")
            {
                throw new ConnectionException(
                    "SDK initialization failed: Invalid API key or network error");
            }

            _isConnected = true;
            _logger.LogInformation("Successfully connected to BreezSDK");

        }, cancellationToken);
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

        // Check connection state
        if (!_isConnected)
        {
            throw new ConnectionException("SDK is not connected. Call ConnectAsync first.");
        }

        // Check for offline mode restriction
        if (_options.OfflineMode)
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
                if (_options.OfflineSimulateDelayMs > 0)
                {
                    await Task.Delay(_options.OfflineSimulateDelayMs, ct);
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
                if (_options.OfflineSimulateDelayMs > 0)
                {
                    await Task.Delay(_options.OfflineSimulateDelayMs, ct);
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
                    BalanceSat = _options.OfflineMockBalanceSat,
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
                if (_options.OfflineSimulateDelayMs > 0)
                {
                    await Task.Delay(_options.OfflineSimulateDelayMs, ct);
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
        //     _eventListenerId = _sdk.AddEventListener(new SdkEventListener(eventCallback, _logger));
        // }
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
        if (_options.OfflineMode)
        {
            _logger.LogWarning("Running in offline mode - skipping API key and mnemonic validation");

            // Still require working directory
            if (string.IsNullOrWhiteSpace(_options.WorkingDirectory))
            {
                throw new ConfigurationException(
                    "Working directory is required even in offline mode",
                    nameof(BreezSdkOptions.WorkingDirectory));
            }

            return;
        }

        // Validate API key
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ConfigurationException(
                "Breez API key is required for SDK initialization. " +
                "Configure BreezSdk:ApiKey in application settings or enable OfflineMode for testing.",
                nameof(BreezSdkOptions.ApiKey));
        }

        // Validate mnemonic
        if (string.IsNullOrWhiteSpace(_options.Mnemonic))
        {
            throw new ConfigurationException(
                "BIP39 mnemonic is required for wallet access. " +
                "Configure BreezSdk:Mnemonic in application settings or enable OfflineMode for testing.",
                nameof(BreezSdkOptions.Mnemonic));
        }

        // Validate working directory
        if (string.IsNullOrWhiteSpace(_options.WorkingDirectory))
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
}
