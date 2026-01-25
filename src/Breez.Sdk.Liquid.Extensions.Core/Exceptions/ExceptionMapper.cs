using Breez.Sdk.Liquid.Extensions.Core.Domain;

namespace Breez.Sdk.Liquid.Extensions.Core.Exceptions;

/// <summary>
/// Maps system and SDK exceptions to the BreezSdkException hierarchy.
/// </summary>
/// <remarks>
/// This class provides intelligent exception mapping that:
/// <list type="bullet">
/// <item><description>Categorizes exceptions by type and message content</description></item>
/// <item><description>Assigns appropriate error codes and retryability flags</description></item>
/// <item><description>Preserves exception chains and messages</description></item>
/// <item><description>Handles case-insensitive message matching</description></item>
/// </list>
/// </remarks>
public static class ExceptionMapper
{
    /// <summary>
    /// Maps an exception to the appropriate BreezSdkException type.
    /// </summary>
    /// <param name="exception">The exception to map.</param>
    /// <returns>A BreezSdkException with appropriate error code and retryability.</returns>
    /// <exception cref="ArgumentNullException">Thrown when exception is null.</exception>
    public static BreezSdkException Map(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // If it's already a BreezSdkException, return as-is
        if (exception is BreezSdkException breezException)
        {
            return breezException;
        }

        // Map based on exception type and message content
        return exception switch
        {
            TimeoutException => MapTimeoutException(exception),
            HttpRequestException => MapHttpRequestException(exception),
            UnauthorizedAccessException => MapUnauthorizedAccessException(exception),
            ArgumentNullException argNull => MapArgumentNullException(argNull),
            ArgumentOutOfRangeException argRange => MapArgumentOutOfRangeException(argRange),
            InvalidOperationException => MapInvalidOperationException(exception),
            _ => MapByMessageContent(exception)
        };
    }

    /// <summary>
    /// Maps TimeoutException to ConnectionException with ConnectionTimeout error code.
    /// </summary>
    private static BreezSdkException MapTimeoutException(Exception exception)
    {
        var message = string.IsNullOrWhiteSpace(exception.Message)
            ? "Connection timeout occurred"
            : $"Connection timeout: {exception.Message}";

        return new ConnectionException(
            BreezErrorCode.ConnectionTimeout,
            message,
            exception);
    }

    /// <summary>
    /// Maps HttpRequestException to TransientException with NetworkError error code.
    /// </summary>
    private static BreezSdkException MapHttpRequestException(Exception exception)
    {
        var message = string.IsNullOrWhiteSpace(exception.Message)
            ? "Network error occurred"
            : exception.Message;

        return new TransientException(
            BreezErrorCode.NetworkError,
            message,
            retryAfter: null,
            exception);
    }

    /// <summary>
    /// Maps UnauthorizedAccessException to ConfigurationException with ConfigurationInvalid error code.
    /// </summary>
    private static BreezSdkException MapUnauthorizedAccessException(Exception exception)
    {
        var message = string.IsNullOrWhiteSpace(exception.Message)
            ? "Unauthorized access - invalid configuration"
            : exception.Message;

        return new ConfigurationException(
            BreezErrorCode.ConfigurationInvalid,
            message,
            propertyName: null,
            exception);
    }

    /// <summary>
    /// Maps ArgumentNullException based on parameter name to specific ConfigurationException types.
    /// </summary>
    private static BreezSdkException MapArgumentNullException(ArgumentNullException exception)
    {
        var paramName = exception.ParamName?.ToLowerInvariant();

        return paramName switch
        {
            "mnemonic" => new ConfigurationException(
                BreezErrorCode.MnemonicMissing,
                exception.Message,
                propertyName: exception.ParamName,
                exception),

            "apikey" => new ConfigurationException(
                BreezErrorCode.ApiKeyMissing,
                exception.Message,
                propertyName: exception.ParamName,
                exception),

            _ => new ConfigurationException(
                BreezErrorCode.ConfigurationMissing,
                exception.Message,
                propertyName: exception.ParamName,
                exception)
        };
    }

    /// <summary>
    /// Maps ArgumentOutOfRangeException based on message content to PaymentException with amount validation error codes.
    /// </summary>
    private static BreezSdkException MapArgumentOutOfRangeException(ArgumentOutOfRangeException exception)
    {
        var message = exception.Message ?? string.Empty;

        if (ContainsIgnoreCase(message, "below minimum"))
        {
            return new PaymentException(
                BreezErrorCode.AmountBelowMinimum,
                message,
                paymentHash: null,
                isRetryable: false,
                exception);
        }

        if (ContainsIgnoreCase(message, "above maximum") || ContainsIgnoreCase(message, "exceeds maximum"))
        {
            return new PaymentException(
                BreezErrorCode.AmountAboveMaximum,
                message,
                paymentHash: null,
                isRetryable: false,
                exception);
        }

        // Generic argument out of range - treat as configuration issue
        return new ConfigurationException(
            BreezErrorCode.ConfigurationInvalid,
            message,
            propertyName: exception.ParamName,
            exception);
    }

    /// <summary>
    /// Maps InvalidOperationException based on message content to appropriate exception types.
    /// </summary>
    private static BreezSdkException MapInvalidOperationException(Exception exception)
    {
        var message = exception.Message ?? string.Empty;

        // Check for connection-related messages
        if (ContainsIgnoreCase(message, "not connected"))
        {
            return new ConnectionException(
                BreezErrorCode.SdkNotConnected,
                message,
                exception);
        }

        // Check for payment-related messages
        if (ContainsIgnoreCase(message, "insufficient funds"))
        {
            return new PaymentException(
                BreezErrorCode.InsufficientFunds,
                message,
                paymentHash: null,
                isRetryable: false,
                exception);
        }

        if (ContainsIgnoreCase(message, "invalid invoice"))
        {
            return new PaymentException(
                BreezErrorCode.InvalidInvoice,
                message,
                paymentHash: null,
                isRetryable: false,
                exception);
        }

        // Check for transient issues in message
        var mappedByMessage = TryMapByMessageContent(exception);
        if (mappedByMessage != null)
        {
            return mappedByMessage;
        }

        // Generic invalid operation - not retryable
        return new BreezSdkException(
            BreezErrorCode.ConnectionError,
            string.IsNullOrEmpty(message) ? "Invalid operation" : message,
            isRetryable: false,
            exception);
    }

    /// <summary>
    /// Maps exceptions based on message content keywords.
    /// </summary>
    private static BreezSdkException MapByMessageContent(Exception exception)
    {
        var mapped = TryMapByMessageContent(exception);
        if (mapped != null)
        {
            return mapped;
        }

        // Fallback: generic BreezSdkException (not retryable)
        var message = string.IsNullOrWhiteSpace(exception.Message)
            ? "An unexpected error occurred"
            : exception.Message;

        return new BreezSdkException(
            BreezErrorCode.ConnectionError,
            message,
            isRetryable: false,
            exception);
    }

    /// <summary>
    /// Attempts to map an exception based on message content keywords.
    /// Returns null if no specific mapping is found.
    /// </summary>
    private static BreezSdkException? TryMapByMessageContent(Exception exception)
    {
        var message = exception.Message ?? string.Empty;

        // Rate limiting
        if (ContainsIgnoreCase(message, "rate limit"))
        {
            return new TransientException(
                BreezErrorCode.RateLimited,
                message,
                retryAfter: null,
                exception);
        }

        // Service availability
        if (ContainsIgnoreCase(message, "service unavailable") ||
            ContainsIgnoreCase(message, "unavailable"))
        {
            return new TransientException(
                BreezErrorCode.ServiceUnavailable,
                message,
                retryAfter: null,
                exception);
        }

        // Invoice expiration
        if (ContainsIgnoreCase(message, "expired"))
        {
            return new PaymentException(
                BreezErrorCode.InvoiceExpired,
                message,
                paymentHash: null,
                isRetryable: false,
                exception);
        }

        // Disconnection
        if (ContainsIgnoreCase(message, "disconnected"))
        {
            return new ConnectionException(
                BreezErrorCode.SdkDisconnected,
                message,
                exception);
        }

        // Insufficient funds
        if (ContainsIgnoreCase(message, "insufficient funds"))
        {
            return new PaymentException(
                BreezErrorCode.InsufficientFunds,
                message,
                paymentHash: null,
                isRetryable: false,
                exception);
        }

        // Invalid invoice
        if (ContainsIgnoreCase(message, "invalid invoice"))
        {
            return new PaymentException(
                BreezErrorCode.InvalidInvoice,
                message,
                paymentHash: null,
                isRetryable: false,
                exception);
        }

        return null;
    }

    /// <summary>
    /// Performs case-insensitive string containment check.
    /// </summary>
    private static bool ContainsIgnoreCase(string source, string value)
    {
        return source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }
}
