namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;

/// <summary>
/// Exception thrown when the wallet has insufficient balance for an operation.
/// </summary>
public class InsufficientBalanceException : LightningPaymentsException
{
    private const string DefaultErrorCode = "INSUFFICIENT_BALANCE";

    /// <summary>
    /// The amount requested in satoshis.
    /// </summary>
    public ulong? RequestedAmountSat { get; }

    /// <summary>
    /// The available balance in satoshis.
    /// </summary>
    public ulong? AvailableBalanceSat { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InsufficientBalanceException"/> class.
    /// </summary>
    public InsufficientBalanceException()
        : base("Insufficient wallet balance for the requested operation.", DefaultErrorCode)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InsufficientBalanceException"/> class
    /// with a specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InsufficientBalanceException(string message)
        : base(message, DefaultErrorCode)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InsufficientBalanceException"/> class
    /// with balance details.
    /// </summary>
    /// <param name="requestedAmountSat">The amount requested in satoshis.</param>
    /// <param name="availableBalanceSat">The available balance in satoshis.</param>
    public InsufficientBalanceException(ulong requestedAmountSat, ulong availableBalanceSat)
        : base($"Insufficient balance. Requested: {requestedAmountSat} sats, Available: {availableBalanceSat} sats.", DefaultErrorCode)
    {
        RequestedAmountSat = requestedAmountSat;
        AvailableBalanceSat = availableBalanceSat;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InsufficientBalanceException"/> class
    /// with a specified message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public InsufficientBalanceException(string message, System.Exception innerException)
        : base(message, DefaultErrorCode, innerException)
    {
    }
}
