using Breez.Sdk.Liquid.Extensions.Core.Abstractions;

namespace Breez.Sdk.Liquid.Extensions.TestUtilities.Fakes;

/// <summary>
/// Fake implementation of <see cref="IBreezSdkWrapper"/> for testing without the real SDK.
/// Provides in-memory simulation of BreezSDK operations for unit testing.
/// </summary>
public class FakeBreezSdkWrapper : IBreezSdkWrapper
{
    private readonly List<SdkPayment> _payments = new();
    private readonly List<Action<SdkEvent>> _eventCallbacks = new();
    private bool _isConnected;
    private ulong _balance = 100_000;
    private int _invoiceCounter;

    /// <summary>
    /// Gets whether the fake SDK is connected.
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// Gets or sets the simulated wallet balance in satoshis.
    /// </summary>
    public ulong Balance
    {
        get => _balance;
        set => _balance = value;
    }

    /// <summary>
    /// Gets or sets an exception to throw from <see cref="PrepareReceivePaymentAsync"/>.
    /// Use this to simulate SDK errors in tests.
    /// </summary>
    public Exception? PrepareReceivePaymentException { get; set; }

    /// <summary>
    /// Gets or sets the simulated delay for async operations.
    /// Default is <see cref="TimeSpan.Zero"/> (no delay).
    /// </summary>
    public TimeSpan SimulatedDelay { get; set; } = TimeSpan.Zero;

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<SdkReceivePaymentResponse> PrepareReceivePaymentAsync(
        ulong amountSat,
        string? description,
        uint? expirySec,
        CancellationToken cancellationToken = default)
    {
        if (SimulatedDelay > TimeSpan.Zero)
        {
            await Task.Delay(SimulatedDelay, cancellationToken);
        }

        if (PrepareReceivePaymentException is not null)
        {
            throw PrepareReceivePaymentException;
        }

        var paymentHash = GeneratePaymentHash();
        var invoice = GenerateInvoice(paymentHash: paymentHash, amountSat: amountSat);
        var expiryTimestamp = DateTimeOffset.UtcNow.AddSeconds(expirySec ?? 3600).ToUnixTimeSeconds();

        return new SdkReceivePaymentResponse
        {
            PaymentHash = paymentHash,
            Invoice = invoice,
            ExpiryTimestamp = expiryTimestamp,
            FeesSat = 0
        };
    }

    /// <inheritdoc />
    public Task<SdkWalletInfo> GetWalletInfoAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new SdkWalletInfo
        {
            BalanceSat = _balance,
            PendingReceiveSat = 0,
            PendingSendSat = 0
        });
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SdkPayment>> ListPaymentsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<SdkPayment>>(_payments.AsReadOnly());
    }

    /// <inheritdoc />
    public void RegisterEventCallback(Action<SdkEvent> eventCallback)
    {
        _eventCallbacks.Add(eventCallback);
    }

    /// <summary>
    /// Simulates receiving a payment by adding it to history, updating balance, and firing events.
    /// </summary>
    /// <param name="paymentHash">The payment hash from the invoice.</param>
    /// <param name="amountSat">Amount received in satoshis.</param>
    /// <param name="preimage">Optional preimage. If null, a random one is generated.</param>
    public void SimulatePaymentReceived(string paymentHash, ulong amountSat, string? preimage = null)
    {
        var payment = new SdkPayment
        {
            PaymentHash = paymentHash,
            AmountSat = amountSat,
            Status = "succeeded",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Preimage = preimage ?? GeneratePreimage()
        };
        _payments.Add(payment);
        _balance += amountSat;

        var sdkEvent = new SdkEvent
        {
            EventType = "PaymentReceived",
            PaymentHash = paymentHash,
            Data = payment
        };

        foreach (var callback in _eventCallbacks)
        {
            callback(sdkEvent);
        }
    }

    /// <summary>
    /// Adds a payment to the payment history without triggering events.
    /// Use this to set up test data.
    /// </summary>
    /// <param name="payment">The payment to add.</param>
    public void AddPayment(SdkPayment payment) => _payments.Add(payment);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _isConnected = false;
        _eventCallbacks.Clear();
        return ValueTask.CompletedTask;
    }

    private string GeneratePaymentHash()
    {
        var counter = Interlocked.Increment(ref _invoiceCounter);
        var hash = $"fakehash{counter:D8}{Guid.NewGuid():N}";
        return hash.Length >= 64 ? hash[..64] : hash.PadRight(64, '0');
    }

    private static string GenerateInvoice(string paymentHash, ulong amountSat)
    {
        var hashPart = paymentHash.Length >= 32 ? paymentHash[..32] : paymentHash.PadRight(32, '0');
        return $"lnbc{amountSat}n1fake{hashPart}";
    }

    private static string GeneratePreimage()
    {
        return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    }
}
