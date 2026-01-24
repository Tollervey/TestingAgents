using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Breez.Sdk.Liquid.Extensions.Core.Infrastructure;

/// <summary>
/// Offline/mock implementation of IBreezSdkService for development without network.
/// </summary>
/// <remarks>
/// This implementation provides simulated SDK behavior for development and testing scenarios.
/// It does not require actual BreezSDK connectivity and operates entirely in-memory with
/// configurable simulation of delays and failures.
///
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item>Immediate connection (simulated)</item>
///   <item>Synthetic invoice generation with unique payment hashes</item>
///   <item>Configurable mock balance</item>
///   <item>Optional operation delays (simulates network latency)</item>
///   <item>Optional random failures (simulates network errors)</item>
///   <item>Payment state persistence via IPaymentRepository</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Configuration Options:</b>
/// <list type="bullet">
///   <item><c>OfflineMockBalanceSat</c>: Mock wallet balance (default: 100,000 sats)</item>
///   <item><c>OfflineSimulateDelayMs</c>: Simulated operation delay in milliseconds (default: 0)</item>
///   <item><c>OfflineSimulateFailureRate</c>: Random failure probability 0.0-1.0 (default: 0.0)</item>
/// </list>
/// </para>
/// </remarks>
public class OfflineBreezSdkService : IBreezSdkService
{
    private readonly IOptions<BreezSdkOptions> _options;
    private readonly IPaymentRepository _repository;
    private readonly ILogger<OfflineBreezSdkService> _logger;
    private readonly Random _random = new();
    private bool _isConnected;
    private int _invoiceCounter;

    /// <summary>
    /// Initializes a new instance of the <see cref="OfflineBreezSdkService"/> class.
    /// </summary>
    /// <param name="options">SDK configuration options.</param>
    /// <param name="repository">Payment state repository.</param>
    /// <param name="logger">Logger instance.</param>
    public OfflineBreezSdkService(
        IOptions<BreezSdkOptions> options,
        IPaymentRepository repository,
        ILogger<OfflineBreezSdkService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsConnected => _isConnected;

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Offline mode: Simulating SDK connection");
        _isConnected = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Offline mode: Simulating SDK disconnection");
        _isConnected = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_isConnected);
    }

    /// <inheritdoc />
    public async Task<OperationResult<Invoice>> CreateInvoiceAsync(
        ulong amountSat,
        string? description = null,
        uint? expirySec = null,
        CancellationToken cancellationToken = default)
    {
        // Check connection state
        if (!_isConnected)
        {
            _logger.LogWarning("Offline mode: Attempted to create invoice while not connected");
            return OperationResult<Invoice>.Failure(new OperationError
            {
                Code = BreezErrorCode.ConnectionError,
                Message = "SDK is not connected. Call ConnectAsync first.",
                IsRetryable = false
            });
        }

        // Simulate delay if configured
        var delayMs = _options.Value.OfflineSimulateDelayMs;
        if (delayMs > 0)
        {
            _logger.LogDebug("Offline mode: Simulating {DelayMs}ms operation delay", delayMs);
            await Task.Delay(delayMs, cancellationToken);
        }

        // Simulate random failure if configured
        var failureRate = _options.Value.OfflineSimulateFailureRate;
        if (failureRate > 0 && _random.NextDouble() < failureRate)
        {
            _logger.LogWarning("Offline mode: Simulated invoice creation failure (failure rate: {FailureRate})", failureRate);
            return OperationResult<Invoice>.Failure(new OperationError
            {
                Code = BreezErrorCode.PaymentFailed,
                Message = "Simulated payment failure for testing",
                IsRetryable = true
            });
        }

        // Generate unique payment hash
        var counter = Interlocked.Increment(ref _invoiceCounter);
        var guid = Guid.NewGuid().ToString("N");
        var paymentHash = $"offlinehash{counter:D8}{guid}";
        if (paymentHash.Length > 64)
        {
            paymentHash = paymentHash.Substring(0, 64);
        }

        // Calculate expiry time
        var expirySeconds = expirySec ?? 3600u; // Default 1 hour
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expirySeconds);

        // Generate synthetic BOLT11-style invoice string
        var destination = $"lnbc{amountSat}n1{paymentHash.Substring(0, Math.Min(20, paymentHash.Length))}";

        // Create invoice object
        var invoice = new Invoice
        {
            PaymentHash = paymentHash,
            Destination = destination,
            AmountSat = amountSat,
            Description = description,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Persist payment state
        var paymentState = new PaymentState
        {
            PaymentHash = paymentHash,
            AmountSat = amountSat,
            Description = description,
            Invoice = destination,
            Status = PaymentStatus.Pending,
            ExpiresAt = expiresAt,
            CreatedAt = invoice.CreatedAt
        };

        try
        {
            await _repository.AddAsync(paymentState, cancellationToken);
            _logger.LogInformation("Offline mode: Created invoice for {AmountSat} sats with payment hash {PaymentHash}",
                amountSat, paymentHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Offline mode: Failed to persist payment state");
            return OperationResult<Invoice>.Failure(new OperationError
            {
                Code = BreezErrorCode.PersistenceFailed,
                Message = "Failed to persist payment state",
                IsRetryable = true,
                Exception = ex
            });
        }

        return OperationResult<Invoice>.Success(invoice);
    }

    /// <inheritdoc />
    public async Task<OperationResult<ulong>> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        // Check connection state
        if (!_isConnected)
        {
            _logger.LogWarning("Offline mode: Attempted to get balance while not connected");
            return OperationResult<ulong>.Failure(new OperationError
            {
                Code = BreezErrorCode.ConnectionError,
                Message = "SDK is not connected. Call ConnectAsync first.",
                IsRetryable = false
            });
        }

        var balance = _options.Value.OfflineMockBalanceSat;
        _logger.LogDebug("Offline mode: Returning mock balance of {Balance} sats", balance);

        return await Task.FromResult(OperationResult<ulong>.Success(balance));
    }

    /// <inheritdoc />
    public async Task<PaymentState?> GetPaymentByHashAsync(
        string paymentHash,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Offline mode: Querying payment by hash {PaymentHash}", paymentHash);
        return await _repository.GetByHashAsync(paymentHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentState>> GetPaymentHistoryAsync(
        int offset = 0,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Offline mode: Querying payment history (offset: {Offset}, limit: {Limit})", offset, limit);
        var payments = await _repository.GetAllAsync(offset, limit, cancellationToken);
        return payments;
    }
}
