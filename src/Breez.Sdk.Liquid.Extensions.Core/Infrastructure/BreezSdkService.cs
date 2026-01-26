using System.Diagnostics;
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.Core.Domain.Events;
using Breez.Sdk.Liquid.Extensions.Core.Exceptions;
using Breez.Sdk.Liquid.Extensions.Core.Observability;
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
    private const int MaxPaymentHistoryLimit = 1000;

    private readonly IBreezSdkWrapper _wrapper;
    private readonly IPaymentRepository _repository;
    private readonly IPaymentEventChannel? _eventChannel;
    private readonly BreezSdkOptions _options;
    private readonly ILogger<BreezSdkService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BreezSdkService"/> class.
    /// </summary>
    /// <param name="wrapper">The SDK wrapper for low-level SDK operations.</param>
    /// <param name="repository">The payment repository for state persistence.</param>
    /// <param name="options">Configuration options for BreezSDK.</param>
    /// <param name="logger">Logger for structured logging.</param>
    public BreezSdkService(
        IBreezSdkWrapper wrapper,
        IPaymentRepository repository,
        IOptions<BreezSdkOptions> options,
        ILogger<BreezSdkService> logger)
        : this(wrapper, repository, null, options, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BreezSdkService"/> class with event publishing support.
    /// </summary>
    /// <param name="wrapper">The SDK wrapper for low-level SDK operations.</param>
    /// <param name="repository">The payment repository for state persistence.</param>
    /// <param name="eventChannel">The event channel for publishing payment events (optional).</param>
    /// <param name="options">Configuration options for BreezSDK.</param>
    /// <param name="logger">Logger for structured logging.</param>
    public BreezSdkService(
        IBreezSdkWrapper wrapper,
        IPaymentRepository repository,
        IPaymentEventChannel? eventChannel,
        IOptions<BreezSdkOptions> options,
        ILogger<BreezSdkService> logger)
    {
        _wrapper = wrapper ?? throw new ArgumentNullException(nameof(wrapper));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _eventChannel = eventChannel;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsConnected => _wrapper.IsConnected;

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.StartConnectActivity();
        var network = _options.Network.ToString().ToLowerInvariant();
        activity?.SetTag("breez.network", network);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Connecting to BreezSDK...");

        // Validate configuration
        ValidateConfiguration();

        try
        {
            await _wrapper.ConnectAsync(cancellationToken);
            _logger.LogInformation("Successfully connected to BreezSDK");

            activity?.SetStatus(ActivityStatusCode.Ok);
            BreezSdkMetrics.SetConnectionState(true, network);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to BreezSDK");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("exception.type", ex.GetType().FullName);
            activity?.AddTag("exception.message", ex.Message);
            BreezSdkMetrics.SetConnectionState(false, network);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            BreezSdkMetrics.RecordOperationDuration("connect", network, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.StartDisconnectActivity();
        var network = _options.Network.ToString().ToLowerInvariant();
        activity?.SetTag("breez.network", network);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Disconnecting from BreezSDK...");

        try
        {
            await _wrapper.DisconnectAsync(cancellationToken);
            _logger.LogInformation("Successfully disconnected from BreezSDK");

            activity?.SetStatus(ActivityStatusCode.Ok);
            BreezSdkMetrics.SetConnectionState(false, network);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during BreezSDK disconnect (non-fatal)");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("exception.type", ex.GetType().FullName);
            activity?.AddTag("exception.message", ex.Message);
            // Don't throw - disconnection errors are typically non-fatal
        }
        finally
        {
            stopwatch.Stop();
            BreezSdkMetrics.RecordOperationDuration("disconnect", network, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <inheritdoc />
    public async Task<OperationResult<Invoice>> CreateInvoiceAsync(
        ulong amountSat,
        string? description = null,
        uint? expirySec = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.StartCreateInvoiceActivity();
        var network = _options.Network.ToString().ToLowerInvariant();
        activity?.SetTag("breez.amount_sat", amountSat);
        activity?.SetTag("breez.network", network);

        var stopwatch = Stopwatch.StartNew();

        // Check connection state
        if (!_wrapper.IsConnected)
        {
            _logger.LogWarning("Cannot create invoice: SDK not connected");
            activity?.SetStatus(ActivityStatusCode.Error, "SDK not connected");
            stopwatch.Stop();
            BreezSdkMetrics.RecordOperationDuration("create_invoice", network, stopwatch.ElapsedMilliseconds);
            BreezSdkMetrics.RecordInvoiceCreated(network, "failure");
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
            activity?.SetStatus(ActivityStatusCode.Error, "Amount must be greater than zero");
            stopwatch.Stop();
            BreezSdkMetrics.RecordOperationDuration("create_invoice", network, stopwatch.ElapsedMilliseconds);
            BreezSdkMetrics.RecordInvoiceCreated(network, "failure");
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
            activity?.SetStatus(ActivityStatusCode.Error, "Amount exceeds maximum");
            stopwatch.Stop();
            BreezSdkMetrics.RecordOperationDuration("create_invoice", network, stopwatch.ElapsedMilliseconds);
            BreezSdkMetrics.RecordInvoiceCreated(network, "failure");
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
            activity?.SetStatus(ActivityStatusCode.Error, "Description too long");
            stopwatch.Stop();
            BreezSdkMetrics.RecordOperationDuration("create_invoice", network, stopwatch.ElapsedMilliseconds);
            BreezSdkMetrics.RecordInvoiceCreated(network, "failure");
            return OperationResult<Invoice>.Failure(new OperationError
            {
                Code = BreezErrorCode.InvalidInvoice,
                Message = $"Invoice description is too long (max {_options.MaxInvoiceDescriptionLength} characters)",
                IsRetryable = false
            });
        }

        // Use default expiry if not provided
        var effectiveExpirySec = expirySec ?? ConfigurationConstants.DefaultInvoiceExpirySec;

        // Create correlation ID for this operation
        var correlationId = Guid.NewGuid().ToString();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Operation"] = "CreateInvoice"
        }))
        {
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
                var createdAt = DateTimeOffset.UtcNow;
                var invoice = new Invoice
                {
                    PaymentHash = sdkResponse.PaymentHash,
                    Destination = sdkResponse.Invoice,
                    Type = InvoiceType.Bolt11,
                    AmountSat = amountSat,
                    Description = description,
                    CreatedAt = createdAt,
                    ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(sdkResponse.ExpiryTimestamp),
                    FeeSat = sdkResponse.FeesSat ?? 0
                };

                // Create and persist payment state
                var paymentState = new PaymentState
                {
                    PaymentHash = sdkResponse.PaymentHash,
                    Status = PaymentStatus.Pending,
                    AmountSat = amountSat,
                    FeeSat = sdkResponse.FeesSat ?? 0,
                    Description = description,
                    CreatedAt = createdAt,
                    ExpiresAt = invoice.ExpiresAt
                };
                await _repository.AddAsync(paymentState, cancellationToken);

                // Publish InvoiceCreated event
                if (_eventChannel != null)
                {
                    var invoiceCreatedEvent = new InvoiceCreated
                    {
                        PaymentHash = sdkResponse.PaymentHash,
                        Invoice = sdkResponse.Invoice,
                        AmountSat = amountSat,
                        Description = description,
                        ExpiresAt = invoice.ExpiresAt,
                        Timestamp = createdAt,
                        CorrelationId = correlationId
                    };
                    await _eventChannel.PublishAsync(invoiceCreatedEvent, cancellationToken);
                    _logger.LogDebug("Published InvoiceCreated event for {PaymentHash}", invoice.PaymentHash);
                }

                _logger.LogInformation(
                    "Successfully created invoice {PaymentHash} for {AmountSat} sat (fees: {FeeSat} sat)",
                    invoice.PaymentHash,
                    invoice.AmountSat,
                    invoice.FeeSat);

                activity?.SetTag("breez.payment_hash", invoice.PaymentHash);
                activity?.SetTag("breez.fee_sat", invoice.FeeSat);
                activity?.SetStatus(ActivityStatusCode.Ok);
                BreezSdkMetrics.RecordInvoiceCreated(network, "success");

                return OperationResult<Invoice>.Success(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create invoice for {AmountSat} sat", amountSat);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddTag("exception.type", ex.GetType().FullName);
                activity?.AddTag("exception.message", ex.Message);
                BreezSdkMetrics.RecordInvoiceCreated(network, "failure");
                return OperationResult<Invoice>.Failure(ex, isRetryable: IsRetryable(ex));
            }
            finally
            {
                stopwatch.Stop();
                BreezSdkMetrics.RecordOperationDuration("create_invoice", network, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    /// <inheritdoc />
    public async Task<PaymentState?> GetPaymentByHashAsync(
        string paymentHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentHash);

        using var activity = ActivitySources.StartActivity(ActivitySources.Operations.GetPayment);
        var network = _options.Network.ToString().ToLowerInvariant();
        activity?.SetTag("breez.payment_hash", paymentHash);
        activity?.SetTag("breez.network", network);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Retrieving payment by hash: {PaymentHash}", paymentHash);

        try
        {
            var result = await _repository.GetByHashAsync(paymentHash, cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("exception.type", ex.GetType().FullName);
            activity?.AddTag("exception.message", ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            BreezSdkMetrics.RecordOperationDuration("get_payment", network, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentState>> GetPaymentHistoryAsync(
        int offset = 0,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, MaxPaymentHistoryLimit);

        using var activity = ActivitySources.StartActivity(ActivitySources.Operations.ListPayments);
        var network = _options.Network.ToString().ToLowerInvariant();
        activity?.SetTag("breez.offset", offset);
        activity?.SetTag("breez.limit", limit);
        activity?.SetTag("breez.network", network);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug(
            "Retrieving payment history with offset={Offset}, limit={Limit}",
            offset,
            limit);

        try
        {
            var result = await _repository.GetAllAsync(offset, limit, cancellationToken);
            activity?.SetTag("breez.result_count", result.Count);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("exception.type", ex.GetType().FullName);
            activity?.AddTag("exception.message", ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            BreezSdkMetrics.RecordOperationDuration("list_payments", network, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <inheritdoc />
    public async Task<OperationResult<ulong>> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.StartGetBalanceActivity();
        var network = _options.Network.ToString().ToLowerInvariant();
        activity?.SetTag("breez.network", network);

        var stopwatch = Stopwatch.StartNew();

        // Create correlation ID for this operation
        var correlationId = Guid.NewGuid().ToString();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Operation"] = "GetBalance"
        }))
        {
            // Check connection state
            if (!_wrapper.IsConnected)
            {
                _logger.LogWarning("Cannot get balance: SDK not connected");
                activity?.SetStatus(ActivityStatusCode.Error, "SDK not connected");
                stopwatch.Stop();
                BreezSdkMetrics.RecordOperationDuration("get_balance", network, stopwatch.ElapsedMilliseconds);
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

                activity?.SetTag("breez.balance_sat", walletInfo.BalanceSat);
                activity?.SetStatus(ActivityStatusCode.Ok);

                return OperationResult<ulong>.Success(walletInfo.BalanceSat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve wallet balance");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddTag("exception.type", ex.GetType().FullName);
                activity?.AddTag("exception.message", ex.Message);
                return OperationResult<ulong>.Failure(ex, isRetryable: IsRetryable(ex));
            }
            finally
            {
                stopwatch.Stop();
                BreezSdkMetrics.RecordOperationDuration("get_balance", network, stopwatch.ElapsedMilliseconds);
            }
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
