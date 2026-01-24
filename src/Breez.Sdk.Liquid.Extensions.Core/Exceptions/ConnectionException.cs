using Breez.Sdk.Liquid.Extensions.Core.Domain;

namespace Breez.Sdk.Liquid.Extensions.Core.Exceptions;

/// <summary>
/// Exception thrown when SDK connection fails or is lost.
/// </summary>
/// <remarks>
/// Connection exceptions are marked as retryable by default since transient network issues
/// are common and operations may succeed on retry. Use specific error codes to distinguish
/// between different connection failure scenarios:
/// <list type="bullet">
/// <item><description><see cref="BreezErrorCode.SdkNotConnected"/>: SDK is not currently connected</description></item>
/// <item><description><see cref="BreezErrorCode.ConnectionTimeout"/>: Connection attempt timed out</description></item>
/// <item><description><see cref="BreezErrorCode.ConnectionFailed"/>: Connection attempt failed</description></item>
/// <item><description><see cref="BreezErrorCode.SdkDisconnected"/>: SDK was disconnected unexpectedly</description></item>
/// </list>
/// </remarks>
public class ConnectionException : BreezSdkException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or null if none.</param>
    public ConnectionException(
        string message,
        Exception? innerException = null)
        : base(BreezErrorCode.SdkNotConnected, message, isRetryable: true, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class with a specific error code.
    /// </summary>
    /// <param name="errorCode">The specific connection error code (should be in the 2xxx range).</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or null if none.</param>
    public ConnectionException(
        BreezErrorCode errorCode,
        string message,
        Exception? innerException = null)
        : base(errorCode, message, isRetryable: true, innerException)
    {
    }
}
