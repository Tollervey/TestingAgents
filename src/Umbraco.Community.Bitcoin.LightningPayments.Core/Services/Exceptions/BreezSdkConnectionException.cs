namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;

/// <summary>
/// Exception thrown when there is a connection issue with the Breez SDK.
/// </summary>
public class BreezSdkConnectionException : LightningPaymentsException
{
    private const string DefaultErrorCode = "BREEZ_SDK_CONNECTION_ERROR";

    /// <summary>
    /// The connection state at the time of the error.
    /// </summary>
    public string? ConnectionState { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BreezSdkConnectionException"/> class.
    /// </summary>
    public BreezSdkConnectionException()
        : base("Failed to connect to Breez SDK.", DefaultErrorCode)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BreezSdkConnectionException"/> class
    /// with a specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BreezSdkConnectionException(string message)
        : base(message, DefaultErrorCode)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BreezSdkConnectionException"/> class
    /// with a specified message and connection state.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="connectionState">The connection state at the time of the error.</param>
    public BreezSdkConnectionException(string message, string connectionState)
        : base(message, DefaultErrorCode)
    {
        ConnectionState = connectionState;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BreezSdkConnectionException"/> class
    /// with a specified message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public BreezSdkConnectionException(string message, System.Exception innerException)
        : base(message, DefaultErrorCode, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BreezSdkConnectionException"/> class
    /// with a specified message, connection state, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="connectionState">The connection state at the time of the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public BreezSdkConnectionException(string message, string connectionState, System.Exception innerException)
        : base(message, DefaultErrorCode, innerException)
    {
        ConnectionState = connectionState;
    }
}
