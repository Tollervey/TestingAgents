using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.Core.Exceptions;

namespace Breez.Sdk.Liquid.Extensions.Core.Abstractions;

/// <summary>
/// Core service interface for BreezSDK operations.
/// </summary>
/// <remarks>
/// This is the primary abstraction for interacting with the BreezSDK Liquid implementation.
/// It provides high-level operations for invoice creation, payment management, and wallet balance queries.
/// All operations are asynchronous and follow the async/await pattern for optimal performance.
/// </remarks>
public interface IBreezSdkService
{
    /// <summary>
    /// Gets a value indicating whether the SDK is currently connected.
    /// </summary>
    /// <remarks>
    /// This is a synchronous property that returns the last known connection state.
    /// For a real-time health check, use <see cref="IsConnectedAsync"/>.
    /// </remarks>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the BreezSDK.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous connect operation.</returns>
    /// <remarks>
    /// This method initializes the connection to the BreezSDK network.
    /// It should be called once during application startup, typically in a hosted service.
    /// If the SDK is already connected, this operation is a no-op.
    /// </remarks>
    /// <exception cref="ConfigurationException">Thrown when SDK configuration is invalid.</exception>
    /// <exception cref="ConnectionException">Thrown when connection to the SDK network fails.</exception>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the BreezSDK.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous disconnect operation.</returns>
    /// <remarks>
    /// This method gracefully shuts down the SDK connection.
    /// It should be called during application shutdown to ensure proper cleanup.
    /// If the SDK is not connected, this operation is a no-op.
    /// </remarks>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Lightning invoice for receiving payments.
    /// </summary>
    /// <param name="amountSat">
    /// The amount to request in satoshis.
    /// <list type="bullet">
    ///   <item>Must be greater than 0</item>
    ///   <item>Must be less than or equal to 2,100,000,000,000,000 satoshis (21 million BTC)</item>
    ///   <item>Should align with network minimum relay fees (typically 1 sat minimum)</item>
    /// </list>
    /// </param>
    /// <param name="description">
    /// Optional human-readable description of the payment purpose.
    /// Maximum 639 bytes (BOLT11 spec limit). Recommended maximum: 500 UTF-8 characters.
    /// </param>
    /// <param name="expirySec">Optional expiry time in seconds. If not specified, uses SDK default (typically 3600 seconds / 1 hour).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the created <see cref="Invoice"/> on success,
    /// or an <see cref="OperationError"/> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The created invoice can be shared with payers via QR code, copy/paste, or Lightning address.
    /// The invoice will expire after the specified duration, defaulting to the SDK's configured expiry time.
    /// Payment state will be tracked automatically and events will be raised on status changes.
    /// </para>
    /// <para>
    /// <b>Validation:</b> Implementations MUST validate input parameters and return
    /// <see cref="OperationResult{T}.Failure(OperationError)"/> with appropriate error codes
    /// for invalid inputs rather than throwing exceptions.
    /// </para>
    /// </remarks>
    /// <exception cref="ConnectionException">Thrown when the SDK is not connected.</exception>
    /// <exception cref="PaymentException">Thrown when invoice creation fails due to SDK errors.</exception>
    Task<OperationResult<Invoice>> CreateInvoiceAsync(
        ulong amountSat,
        string? description = null,
        uint? expirySec = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a payment by its payment hash.
    /// </summary>
    /// <param name="paymentHash">The hex-encoded payment hash that uniquely identifies the payment.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The <see cref="PaymentState"/> if found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// The payment hash is a unique identifier for Lightning payments.
    /// This method queries the persistence layer for stored payment state.
    /// </remarks>
    Task<PaymentState?> GetPaymentByHashAsync(
        string paymentHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated payment history.
    /// </summary>
    /// <param name="offset">The number of records to skip (for pagination). Must be >= 0.</param>
    /// <param name="limit">The maximum number of records to return. Must be between 1 and 1000.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of <see cref="PaymentState"/> records ordered by creation date descending (newest first).
    /// </returns>
    /// <remarks>
    /// This method queries the persistence layer for historical payment records.
    /// Results are ordered by creation timestamp (newest first) to support typical UI scenarios.
    /// For large datasets, use pagination with offset and limit parameters.
    /// </remarks>
    Task<IReadOnlyList<PaymentState>> GetPaymentHistoryAsync(
        int offset = 0,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current wallet balance in satoshis.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the balance in satoshis on success,
    /// or an <see cref="OperationError"/> on failure.
    /// </returns>
    /// <remarks>
    /// The balance represents the total spendable amount in the Lightning wallet.
    /// This queries the SDK directly and may involve network calls.
    /// </remarks>
    /// <exception cref="ConnectionException">Thrown when the SDK is not connected.</exception>
    Task<OperationResult<ulong>> GetBalanceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the SDK connection is healthy.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the SDK is connected and operational; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This is an active health check that verifies the SDK connection status.
    /// Unlike the <see cref="IsConnected"/> property, this method may perform network operations.
    /// Use this for health check endpoints and monitoring scenarios.
    /// </remarks>
    Task<bool> IsConnectedAsync(CancellationToken cancellationToken = default);
}
