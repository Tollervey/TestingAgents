using Breez.Sdk.Liquid.Extensions.Core.Domain;

namespace Breez.Sdk.Liquid.Extensions.Core.Exceptions;

/// <summary>
/// Base exception for all BreezSDK-related errors.
/// Provides structured error information with categorization and retry semantics.
/// </summary>
public class BreezSdkException : Exception
{
    /// <summary>
    /// Gets the error code categorizing this exception.
    /// </summary>
    public BreezErrorCode ErrorCode { get; }

    /// <summary>
    /// Gets a value indicating whether this error is potentially transient and can be retried.
    /// </summary>
    public bool IsRetryable { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BreezSdkException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code categorizing the failure.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="isRetryable">Whether the operation can be retried. Default is false.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or null if none.</param>
    public BreezSdkException(
        BreezErrorCode errorCode,
        string message,
        bool isRetryable = false,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        IsRetryable = isRetryable;
    }
}
