namespace Breez.Sdk.Liquid.Extensions.Core.Abstractions;

/// <summary>
/// Low-level wrapper interface for the BreezSDK bindings.
/// Enables testability by abstracting the native SDK calls.
/// </summary>
public interface IBreezSdkWrapper : IAsyncDisposable
{
    /// <summary>
    /// Gets whether the SDK instance is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// The current connection state.
    /// </summary>
    ConnectionState State { get; }

    /// <summary>
    /// Event fired when the connection state changes.
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// Indicates whether a reconnection attempt is allowed in the current state.
    /// Returns false when in Reconnecting, Failed, or disposed state.
    /// </summary>
    bool CanAttemptReconnect { get; }

    /// <summary>
    /// Initializes and connects the SDK.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects and disposes the SDK instance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Prepares a receive payment (creates invoice).
    /// </summary>
    /// <param name="amountSat">Amount in satoshis.</param>
    /// <param name="description">Payment description.</param>
    /// <param name="expirySec">Expiry time in seconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw SDK response containing invoice data.</returns>
    Task<SdkReceivePaymentResponse> PrepareReceivePaymentAsync(
        ulong amountSat,
        string? description,
        uint? expirySec,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets wallet information including balance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The wallet info response.</returns>
    Task<SdkWalletInfo> GetWalletInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists payments from the SDK.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of raw SDK payment data.</returns>
    Task<IReadOnlyList<SdkPayment>> ListPaymentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a callback for SDK events.
    /// </summary>
    /// <param name="eventCallback">The callback to invoke on events.</param>
    void RegisterEventCallback(Action<SdkEvent> eventCallback);

    /// <summary>
    /// Attempts to reconnect to the SDK with exponential backoff.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if reconnection succeeded, false if all attempts failed.</returns>
    Task<bool> TryReconnectAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a raw SDK receive payment response.
/// </summary>
public record SdkReceivePaymentResponse
{
    /// <summary>
    /// The payment hash.
    /// </summary>
    public required string PaymentHash { get; init; }

    /// <summary>
    /// The invoice string.
    /// </summary>
    public required string Invoice { get; init; }

    /// <summary>
    /// Expiry timestamp in Unix epoch seconds.
    /// </summary>
    public long ExpiryTimestamp { get; init; }

    /// <summary>
    /// Estimated fees in satoshis.
    /// </summary>
    public ulong? FeesSat { get; init; }
}

/// <summary>
/// Represents wallet balance information from the SDK.
/// </summary>
public record SdkWalletInfo
{
    /// <summary>
    /// Balance in satoshis.
    /// </summary>
    public ulong BalanceSat { get; init; }

    /// <summary>
    /// Pending receive amount in satoshis.
    /// </summary>
    public ulong PendingReceiveSat { get; init; }

    /// <summary>
    /// Pending send amount in satoshis.
    /// </summary>
    public ulong PendingSendSat { get; init; }
}

/// <summary>
/// Represents a raw payment from the SDK.
/// </summary>
public record SdkPayment
{
    /// <summary>
    /// The payment hash.
    /// </summary>
    public required string PaymentHash { get; init; }

    /// <summary>
    /// Amount in satoshis.
    /// </summary>
    public ulong AmountSat { get; init; }

    /// <summary>
    /// Fee in satoshis.
    /// </summary>
    public ulong? FeeSat { get; init; }

    /// <summary>
    /// Payment status string from SDK.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Timestamp in Unix epoch seconds.
    /// </summary>
    public long Timestamp { get; init; }

    /// <summary>
    /// Description if available.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Preimage if payment succeeded.
    /// </summary>
    public string? Preimage { get; init; }
}

/// <summary>
/// Represents an SDK event.
/// </summary>
public record SdkEvent
{
    /// <summary>
    /// The event type.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Associated payment hash, if any.
    /// </summary>
    public string? PaymentHash { get; init; }

    /// <summary>
    /// Raw event data.
    /// </summary>
    public object? Data { get; init; }
}
