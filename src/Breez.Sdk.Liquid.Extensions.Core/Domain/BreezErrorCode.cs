namespace Breez.Sdk.Liquid.Extensions.Core.Domain;

/// <summary>
/// Defines error codes for BreezSDK operations, categorized by error type.
/// </summary>
/// <remarks>
/// Error codes are organized into ranges:
/// <list type="bullet">
/// <item><description>1xxx: Configuration errors</description></item>
/// <item><description>2xxx: Connection errors</description></item>
/// <item><description>3xxx: Payment errors</description></item>
/// <item><description>4xxx: Persistence errors</description></item>
/// <item><description>5xxx: Transient errors</description></item>
/// </list>
/// </remarks>
public enum BreezErrorCode
{
    // Configuration errors (1xxx)

    /// <summary>
    /// Required configuration is missing.
    /// </summary>
    ConfigurationMissing = 1001,

    /// <summary>
    /// Configuration values are invalid or malformed.
    /// </summary>
    ConfigurationInvalid = 1002,

    /// <summary>
    /// Mnemonic seed phrase is missing.
    /// </summary>
    MnemonicMissing = 1003,

    /// <summary>
    /// API key is missing.
    /// </summary>
    ApiKeyMissing = 1004,

    // Connection errors (2xxx)

    /// <summary>
    /// SDK is not connected.
    /// </summary>
    SdkNotConnected = 2001,

    /// <summary>
    /// Connection attempt timed out.
    /// </summary>
    ConnectionTimeout = 2002,

    /// <summary>
    /// Connection attempt failed.
    /// </summary>
    ConnectionFailed = 2003,

    /// <summary>
    /// SDK was disconnected unexpectedly.
    /// </summary>
    SdkDisconnected = 2004,

    // Payment errors (3xxx)

    /// <summary>
    /// Failed to create invoice.
    /// </summary>
    InvoiceCreationFailed = 3001,

    /// <summary>
    /// Payment operation failed.
    /// </summary>
    PaymentFailed = 3002,

    /// <summary>
    /// Insufficient funds to complete payment.
    /// </summary>
    InsufficientFunds = 3003,

    /// <summary>
    /// Amount is below the minimum allowed value.
    /// </summary>
    AmountBelowMinimum = 3004,

    /// <summary>
    /// Amount exceeds the maximum allowed value.
    /// </summary>
    AmountAboveMaximum = 3005,

    /// <summary>
    /// Invoice has expired.
    /// </summary>
    InvoiceExpired = 3006,

    /// <summary>
    /// Invoice format is invalid or corrupted.
    /// </summary>
    InvalidInvoice = 3007,

    // Persistence errors (4xxx)

    /// <summary>
    /// Failed to persist data.
    /// </summary>
    PersistenceFailed = 4001,

    /// <summary>
    /// Payment record was not found.
    /// </summary>
    PaymentNotFound = 4002,

    /// <summary>
    /// Payment record already exists (duplicate).
    /// </summary>
    DuplicatePayment = 4003,

    // Transient errors (5xxx)

    /// <summary>
    /// Request was rate limited by the service.
    /// </summary>
    RateLimited = 5001,

    /// <summary>
    /// Service is temporarily unavailable.
    /// </summary>
    ServiceUnavailable = 5002,

    /// <summary>
    /// Network error occurred during operation.
    /// </summary>
    NetworkError = 5003
}
