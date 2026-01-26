namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;

/// <summary>
/// Exception thrown when a Lightning invoice is invalid or malformed.
/// </summary>
public class InvalidInvoiceException : LightningPaymentsException
{
    private const string DefaultErrorCode = "INVALID_INVOICE";

    /// <summary>
    /// The invoice string that was invalid.
    /// </summary>
    public string? Invoice { get; }

    /// <summary>
    /// The specific validation error.
    /// </summary>
    public string? ValidationError { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidInvoiceException"/> class.
    /// </summary>
    public InvalidInvoiceException()
        : base("The provided invoice is invalid.", DefaultErrorCode)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidInvoiceException"/> class
    /// with a specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidInvoiceException(string message)
        : base(message, DefaultErrorCode)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidInvoiceException"/> class
    /// with invoice details.
    /// </summary>
    /// <param name="invoice">The invalid invoice string (may be truncated for security).</param>
    /// <param name="validationError">The specific validation error.</param>
    public InvalidInvoiceException(string? invoice, string validationError)
        : base($"Invalid invoice: {validationError}", DefaultErrorCode)
    {
        // Truncate invoice for security/logging purposes
        Invoice = invoice?.Length > 20 ? invoice[..20] + "..." : invoice;
        ValidationError = validationError;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidInvoiceException"/> class
    /// with a specified message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public InvalidInvoiceException(string message, System.Exception innerException)
        : base(message, DefaultErrorCode, innerException)
    {
    }
}
