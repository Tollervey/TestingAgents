using Breez.Sdk.Liquid.Extensions.Core.Domain;

namespace Breez.Sdk.Liquid.Extensions.Core.Exceptions;

/// <summary>
/// Exception thrown when a payment operation fails.
/// </summary>
/// <remarks>
/// This exception provides payment-specific context including the payment hash
/// when available. Use the <see cref="BreezSdkException.ErrorCode"/> to determine
/// the specific type of payment failure (e.g., insufficient funds, expired invoice).
/// </remarks>
public class PaymentException : BreezSdkException
{
    /// <summary>
    /// Gets the payment hash associated with the failed operation, if available.
    /// </summary>
    public string? PaymentHash { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="paymentHash">The payment hash associated with the failure, if available.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or null if none.</param>
    public PaymentException(
        string message,
        string? paymentHash = null,
        Exception? innerException = null)
        : base(BreezErrorCode.PaymentFailed, message, isRetryable: false, innerException)
    {
        PaymentHash = paymentHash;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentException"/> class with a specific error code.
    /// </summary>
    /// <param name="errorCode">The specific payment error code (e.g., InsufficientFunds, InvalidInvoice).</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="paymentHash">The payment hash associated with the failure, if available.</param>
    /// <param name="isRetryable">Whether the payment operation can be retried. Default is false.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or null if none.</param>
    public PaymentException(
        BreezErrorCode errorCode,
        string message,
        string? paymentHash = null,
        bool isRetryable = false,
        Exception? innerException = null)
        : base(errorCode, message, isRetryable, innerException)
    {
        PaymentHash = paymentHash;
    }
}
