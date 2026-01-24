using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Breez.Sdk.Liquid.Extensions.Core.Infrastructure;

/// <summary>
/// Production implementation of <see cref="IBreezSdkService"/>.
/// Provides high-level operations for BreezSDK Liquid integration including invoice creation,
/// payment management, and balance queries.
/// </summary>
/// <remarks>
/// This service implements the two-step payment pattern required by BreezSDK:
/// 1. Validate input parameters
/// 2. Call SDK wrapper to prepare/execute operations
/// 3. Map SDK responses to domain objects
/// 4. Return OperationResult for safe error handling
/// </remarks>
public class BreezSdkService : IBreezSdkService
{
    private readonly IBreezSdkWrapper _wrapper;
    private readonly BreezSdkOptions _options;
    private readonly ILogger<BreezSdkService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreezSdkService"/> class.
    /// </summary>
    /// <param name="wrapper">The SDK wrapper for low-level SDK operations.</param>
    /// <param name="options">Configuration options for BreezSDK.</param>
    /// <param name="logger">Logger for structured logging.</param>
    public BreezSdkService(
        IBreezSdkWrapper wrapper,
        IOptions<BreezSdkOptions> options,
        ILogger<BreezSdkService> logger)
    {
        _wrapper = wrapper ?? throw new ArgumentNullException(nameof(wrapper));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsConnected => _wrapper.IsConnected;

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Connecting to BreezSDK...");

        // Validate configuration
        ValidateConfiguration();

        try
        {
            await _wrapper.ConnectAsync(cancellationToken);
            _logger.LogInformation("Successfully connected to BreezSDK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to BreezSDK");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Disconnecting from BreezSDK...");

        try
        {
            await _wrapper.DisconnectAsync(cancellationToken);
            _logger.LogInformation("Successfully disconnected from BreezSDK");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during BreezSDK disconnect (non-fatal)");
            // Don't throw - disconnection errors are typically non-fatal
        }
    }

    /// <inheritdoc />
    public async Task<OperationResult<Invoice>> CreateInvoiceAsync(
        ulong amountSat,
        string? description = null,
        uint? expirySec = null,
        CancellationToken cancellationToken = default)
    {
        // Check connection state
        if (!_wrapper.IsConnected)
        {
            _logger.LogWarning("Cannot create invoice: SDK not connected");
            return OperationResult<Invoice>.Failure(new OperationError
            {
                Code = BreezErrorCode.SdkNotConnected,
                Message = "SDK is not connected. Call ConnectAsync() first.",
                IsRetryable = true
            });
        }

        // Validate amount > 0
        if (amountSat == 0)
        {
            _logger.LogWarning("Invoice creation failed: amount must be greater than zero");
            return OperationResult<Invoice>.Failure(new OperationError
            {
                Code = BreezErrorCode.AmountBelowMinimum,
                Message = "Invoice amount must be greater than zero",
                IsRetryable = false
            });
        }

        // Validate amount <= MaxInvoiceAmountSat
        if (amountSat > _options.MaxInvoiceAmountSat)
        {
            _logger.LogWarning(
                "Invoice creation failed: amount {AmountSat} exceeds maximum {MaxAmount}",
                amountSat,
                _options.MaxInvoiceAmountSat);
            return OperationResult<Invoice>.Failure(new OperationError
            {
                Code = BreezErrorCode.AmountAboveMaximum,
                Message = $"Invoice amount {amountSat} sat exceeds maximum allowed {_options.MaxInvoiceAmountSat} sat",
                IsRetryable = false
            });
        }

        // Validate description length if provided
        if (description != null && description.Length > _options.MaxInvoiceDescriptionLength)
        {
            _logger.LogWarning(
                "Invoice creation failed: description length {Length} exceeds maximum {MaxLength}",
                description.Length,
                _options.MaxInvoiceDescriptionLength);
            return OperationResult<Invoice>.Failure(new OperationError
            {
                Code = BreezErrorCode.InvalidInvoice,
                Message = $"Invoice description is too long (max {_options.MaxInvoiceDescriptionLength} characters)",
                IsRetryable = false
            });
        }

        // Use default expiry if not provided
        var effectiveExpirySec = expirySec ?? ConfigurationConstants.DefaultInvoiceExpirySec;

        try
        {
            _logger.LogInformation(
                "Creating invoice for {AmountSat} sat with {ExpirySec}s expiry",
                amountSat,
                effectiveExpirySec);

            // Call wrapper to prepare receive payment
            var sdkResponse = await _wrapper.PrepareReceivePaymentAsync(
                amountSat,
                description,
                effectiveExpirySec,
                cancellationToken);

            // Map SDK response to domain Invoice
            var invoice = new Invoice
            {
                PaymentHash = sdkResponse.PaymentHash,
                Destination = sdkResponse.Invoice,
                Type = InvoiceType.Bolt11,
                AmountSat = amountSat,
                Description = description,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(sdkResponse.ExpiryTimestamp),
                FeeSat = sdkResponse.FeesSat ?? 0
            };

            _logger.LogInformation(
                "Successfully created invoice {PaymentHash} for {AmountSat} sat (fees: {FeeSat} sat)",
                invoice.PaymentHash,
                invoice.AmountSat,
                invoice.FeeSat);

            return OperationResult<Invoice>.Success(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create invoice for {AmountSat} sat", amountSat);
            return OperationResult<Invoice>.Failure(ex, isRetryable: IsRetryable(ex));
        }
    }

    /// <inheritdoc />
    public Task<PaymentState?> GetPaymentByHashAsync(
        string paymentHash,
        CancellationToken cancellationToken = default)
    {
        // NOTE: Repository integration will be added in a future task
        // For now, return null (payment not found)
        _logger.LogDebug("GetPaymentByHashAsync called for {PaymentHash} (not implemented)", paymentHash);
        return Task.FromResult<PaymentState?>(null);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PaymentState>> GetPaymentHistoryAsync(
        int offset = 0,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        // NOTE: Repository integration will be added in a future task
        // For now, return empty list
        _logger.LogDebug(
            "GetPaymentHistoryAsync called with offset={Offset}, limit={Limit} (not implemented)",
            offset,
            limit);
        return Task.FromResult<IReadOnlyList<PaymentState>>(Array.Empty<PaymentState>());
    }

    /// <inheritdoc />
    public async Task<OperationResult<ulong>> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        // Check connection state
        if (!_wrapper.IsConnected)
        {
            _logger.LogWarning("Cannot get balance: SDK not connected");
            return OperationResult<ulong>.Failure(new OperationError
            {
                Code = BreezErrorCode.SdkNotConnected,
                Message = "SDK is not connected. Call ConnectAsync() first.",
                IsRetryable = true
            });
        }

        try
        {
            var walletInfo = await _wrapper.GetWalletInfoAsync(cancellationToken);
            _logger.LogDebug("Retrieved wallet balance: {BalanceSat} sat", walletInfo.BalanceSat);
            return OperationResult<ulong>.Success(walletInfo.BalanceSat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve wallet balance");
            return OperationResult<ulong>.Failure(ex, isRetryable: IsRetryable(ex));
        }
    }

    /// <inheritdoc />
    public Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        var isConnected = _wrapper.IsConnected;
        _logger.LogDebug("IsConnectedAsync: {IsConnected}", isConnected);
        return Task.FromResult(isConnected);
    }

    /// <summary>
    /// Validates the BreezSDK configuration.
    /// </summary>
    /// <exception cref="ConfigurationException">Thrown when configuration is invalid.</exception>
    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ConfigurationException(
                BreezErrorCode.ApiKeyMissing,
                "BreezSDK API key is required",
                propertyName: nameof(BreezSdkOptions.ApiKey));
        }

        if (string.IsNullOrWhiteSpace(_options.Mnemonic))
        {
            throw new ConfigurationException(
                BreezErrorCode.MnemonicMissing,
                "BreezSDK mnemonic is required",
                propertyName: nameof(BreezSdkOptions.Mnemonic));
        }
    }

    /// <summary>
    /// Determines if an exception represents a retryable error.
    /// </summary>
    private static bool IsRetryable(Exception ex) => ex switch
    {
        TransientException => true,
        ConnectionException => true,
        _ => false
    };
}
