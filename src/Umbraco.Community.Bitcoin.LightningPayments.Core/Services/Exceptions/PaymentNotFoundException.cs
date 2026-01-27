namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;

/// <summary>
/// Exception thrown when a payment cannot be found by its identifier.
/// </summary>
public class PaymentNotFoundException : LightningPaymentsException
{
    private const string DefaultErrorCode = "PAYMENT_NOT_FOUND";

    /// <summary>
    /// The payment hash that was not found.
    /// </summary>
    public string? PaymentHash { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentNotFoundException"/> class.
    /// </summary>
    public PaymentNotFoundException()
        : base("Payment not found.", DefaultErrorCode)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentNotFoundException"/> class
    /// with a specified payment hash.
    /// </summary>
    /// <param name="paymentHash">The payment hash that was not found.</param>
    public PaymentNotFoundException(string paymentHash)
        : base($"Payment with hash '{paymentHash}' was not found.", DefaultErrorCode)
    {
        PaymentHash = paymentHash;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentNotFoundException"/> class
    /// with a specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="paymentHash">The payment hash that was not found.</param>
    public PaymentNotFoundException(string message, string? paymentHash)
        : base(message, DefaultErrorCode)
    {
        PaymentHash = paymentHash;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentNotFoundException"/> class
    /// with a specified message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public PaymentNotFoundException(string message, System.Exception innerException)
        : base(message, DefaultErrorCode, innerException)
    {
    }
}
