using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Refund;

/// <summary>
/// Service for managing refund lifecycle (prepare, initiate, query).
/// </summary>
public interface IRefundService
{
    /// <summary>
    /// Prepares a refund by validating the original payment, parsing the destination invoice,
    /// checking balance, and returning a fee estimate.
    /// </summary>
    /// <param name="originalPaymentHash">The payment hash of the original payment to refund.</param>
    /// <param name="destinationInvoice">BOLT11 invoice provided by the customer for the refund.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing validation status and fee estimate.</returns>
    Task<PrepareRefundResult> PrepareRefundAsync(string originalPaymentHash, string destinationInvoice, CancellationToken ct = default);

    /// <summary>
    /// Initiates a refund: creates a record, sends payment via SDK, updates status.
    /// </summary>
    /// <param name="originalPaymentHash">The payment hash of the original payment to refund.</param>
    /// <param name="destinationInvoice">BOLT11 invoice provided by the customer for the refund.</param>
    /// <param name="initiatedByUserId">Umbraco user ID who initiated the refund.</param>
    /// <param name="reason">Optional reason for the refund.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created refund transaction with its status.</returns>
    Task<RefundTransaction> InitiateRefundAsync(string originalPaymentHash, string destinationInvoice, string initiatedByUserId, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a refund by its ID.
    /// </summary>
    /// <param name="refundId">The refund's unique identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The refund, or null if not found.</returns>
    Task<RefundTransaction?> GetRefundByIdAsync(Guid refundId, CancellationToken ct = default);

    /// <summary>
    /// Lists refunds with optional status filter and pagination.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="skip">Number of records to skip for pagination.</param>
    /// <param name="take">Number of records to take for pagination.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple of matching refunds and total count.</returns>
    Task<(IReadOnlyList<RefundTransaction> Items, int Total)> GetRefundsAsync(RefundStatus? status = null, int skip = 0, int take = 20, CancellationToken ct = default);
}

/// <summary>
/// Result of a prepare refund operation.
/// </summary>
public class PrepareRefundResult
{
    /// <summary>
    /// Original payment amount in satoshis.
    /// </summary>
    public ulong OriginalAmountSat { get; init; }

    /// <summary>
    /// Refund amount in satoshis (from invoice or original amount).
    /// </summary>
    public ulong RefundAmountSat { get; init; }

    /// <summary>
    /// Estimated fee in satoshis for the refund payment.
    /// </summary>
    public ulong FeeSat { get; init; }

    /// <summary>
    /// Current wallet balance in satoshis.
    /// </summary>
    public ulong WalletBalanceSat { get; init; }

    /// <summary>
    /// Whether the refund can proceed (all validations passed and sufficient balance).
    /// </summary>
    public bool CanProceed { get; init; }

    /// <summary>
    /// Validation error message if CanProceed is false.
    /// </summary>
    public string? ValidationError { get; init; }
}
