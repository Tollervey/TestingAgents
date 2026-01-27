namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;

/// <summary>
/// Exception thrown when a refund amount exceeds the original payment amount.
/// </summary>
public class RefundExceedsOriginalException : LightningPaymentsException
{
    private const string DefaultErrorCode = "REFUND_EXCEEDS_ORIGINAL";

    /// <summary>
    /// The requested refund amount in satoshis.
    /// </summary>
    public ulong? RefundAmountSat { get; }

    /// <summary>
    /// The original payment amount in satoshis.
    /// </summary>
    public ulong? OriginalAmountSat { get; }

    /// <summary>
    /// The total amount already refunded in satoshis.
    /// </summary>
    public ulong? AlreadyRefundedSat { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefundExceedsOriginalException"/> class.
    /// </summary>
    public RefundExceedsOriginalException()
        : base("Refund amount exceeds the original payment amount.", DefaultErrorCode)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefundExceedsOriginalException"/> class
    /// with a specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public RefundExceedsOriginalException(string message)
        : base(message, DefaultErrorCode)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefundExceedsOriginalException"/> class
    /// with refund details.
    /// </summary>
    /// <param name="refundAmountSat">The requested refund amount in satoshis.</param>
    /// <param name="originalAmountSat">The original payment amount in satoshis.</param>
    public RefundExceedsOriginalException(ulong refundAmountSat, ulong originalAmountSat)
        : base($"Refund amount ({refundAmountSat} sats) exceeds original payment ({originalAmountSat} sats).", DefaultErrorCode)
    {
        RefundAmountSat = refundAmountSat;
        OriginalAmountSat = originalAmountSat;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefundExceedsOriginalException"/> class
    /// with refund details including already refunded amount.
    /// </summary>
    /// <param name="refundAmountSat">The requested refund amount in satoshis.</param>
    /// <param name="originalAmountSat">The original payment amount in satoshis.</param>
    /// <param name="alreadyRefundedSat">The total amount already refunded in satoshis.</param>
    public RefundExceedsOriginalException(ulong refundAmountSat, ulong originalAmountSat, ulong alreadyRefundedSat)
        : base($"Refund amount ({refundAmountSat} sats) plus already refunded ({alreadyRefundedSat} sats) exceeds original payment ({originalAmountSat} sats). Maximum refundable: {originalAmountSat - alreadyRefundedSat} sats.", DefaultErrorCode)
    {
        RefundAmountSat = refundAmountSat;
        OriginalAmountSat = originalAmountSat;
        AlreadyRefundedSat = alreadyRefundedSat;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefundExceedsOriginalException"/> class
    /// with a specified message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public RefundExceedsOriginalException(string message, System.Exception innerException)
        : base(message, DefaultErrorCode, innerException)
    {
    }
}
