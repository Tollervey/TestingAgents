namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;

/// <summary>
/// Base exception for all Lightning Payments related errors.
/// Provides a unified exception hierarchy for consistent error handling and logging.
/// </summary>
public class LightningPaymentsException : System.Exception
{
    /// <summary>
    /// A machine-readable error code for programmatic handling.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LightningPaymentsException"/> class.
    /// </summary>
    public LightningPaymentsException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LightningPaymentsException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public LightningPaymentsException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LightningPaymentsException"/> class
    /// with a specified error message and error code.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">A machine-readable error code.</param>
    public LightningPaymentsException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LightningPaymentsException"/> class
    /// with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public LightningPaymentsException(string message, System.Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LightningPaymentsException"/> class
    /// with a specified error message, error code, and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">A machine-readable error code.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public LightningPaymentsException(string message, string errorCode, System.Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
