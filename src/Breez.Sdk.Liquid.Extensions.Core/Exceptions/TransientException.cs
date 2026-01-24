using Breez.Sdk.Liquid.Extensions.Core.Domain;

namespace Breez.Sdk.Liquid.Extensions.Core.Exceptions;

/// <summary>
/// Exception for transient errors that can be retried.
/// </summary>
/// <remarks>
/// Transient exceptions represent temporary failures such as network timeouts,
/// rate limiting, or service unavailability. These errors should be retried
/// with appropriate backoff strategies.
/// </remarks>
public class TransientException : BreezSdkException
{
    /// <summary>
    /// Gets the suggested delay before retry, if applicable.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransientException"/> class
    /// with the default ServiceUnavailable error code.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="retryAfter">The suggested delay before retry. If null, retry with exponential backoff.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or null if none.</param>
    public TransientException(
        string message,
        TimeSpan? retryAfter = null,
        Exception? innerException = null)
        : base(BreezErrorCode.ServiceUnavailable, message, isRetryable: true, innerException)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransientException"/> class
    /// with a specific transient error code.
    /// </summary>
    /// <param name="errorCode">The specific transient error code (e.g., RateLimited, NetworkError).</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="retryAfter">The suggested delay before retry. If null, retry with exponential backoff.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or null if none.</param>
    public TransientException(
        BreezErrorCode errorCode,
        string message,
        TimeSpan? retryAfter = null,
        Exception? innerException = null)
        : base(errorCode, message, isRetryable: true, innerException)
    {
        RetryAfter = retryAfter;
    }
}
