using Breez.Sdk.Liquid.Extensions.Core.Domain;

namespace Breez.Sdk.Liquid.Extensions.Core.Exceptions;

/// <summary>
/// Exception thrown when BreezSDK configuration is invalid or missing.
/// </summary>
/// <remarks>
/// This exception is not retryable as configuration issues require manual intervention.
/// Use the <see cref="PropertyName"/> property to identify the specific configuration issue.
/// </remarks>
public class ConfigurationException : BreezSdkException
{
    /// <summary>
    /// Gets the name of the configuration property that is invalid or missing.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="propertyName">The name of the invalid or missing configuration property.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or null if none.</param>
    public ConfigurationException(
        string message,
        string? propertyName = null,
        Exception? innerException = null)
        : base(BreezErrorCode.ConfigurationInvalid, message, isRetryable: false, innerException)
    {
        PropertyName = propertyName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specific error code.
    /// </summary>
    /// <param name="errorCode">The specific configuration-related error code.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="propertyName">The name of the invalid or missing configuration property.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or null if none.</param>
    /// <remarks>
    /// Use this constructor when you need to specify a more granular error code such as
    /// <see cref="BreezErrorCode.ConfigurationMissing"/>, <see cref="BreezErrorCode.MnemonicMissing"/>,
    /// or <see cref="BreezErrorCode.ApiKeyMissing"/>.
    /// </remarks>
    public ConfigurationException(
        BreezErrorCode errorCode,
        string message,
        string? propertyName = null,
        Exception? innerException = null)
        : base(errorCode, message, isRetryable: false, innerException)
    {
        PropertyName = propertyName;
    }
}
