namespace Breez.Sdk.Liquid.Extensions.Core.Configuration;

/// <summary>
/// Constants used for BreezSDK configuration.
/// </summary>
public static class ConfigurationConstants
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "BreezSdk";

    /// <summary>
    /// Default invoice expiry time in seconds (1 hour).
    /// </summary>
    public const uint DefaultInvoiceExpirySec = 3600;

    /// <summary>
    /// Maximum theoretical satoshi amount (21 million BTC).
    /// </summary>
    public const ulong MaxSatoshis = 2_100_000_000_000_000;

    /// <summary>
    /// Default maximum invoice amount in satoshis.
    /// </summary>
    public const ulong DefaultMaxInvoiceAmountSat = 10_000_000;

    /// <summary>
    /// Default maximum invoice description length.
    /// </summary>
    public const int DefaultMaxInvoiceDescriptionLength = 200;

    /// <summary>
    /// BOLT11 maximum description length in bytes.
    /// </summary>
    public const int Bolt11MaxDescriptionBytes = 639;

    /// <summary>
    /// Default connection timeout in seconds.
    /// </summary>
    public const int DefaultConnectionTimeoutSeconds = 30;

    /// <summary>
    /// Minimum connection timeout in seconds.
    /// </summary>
    public const int MinConnectionTimeoutSeconds = 5;

    /// <summary>
    /// Maximum connection timeout in seconds.
    /// </summary>
    public const int MaxConnectionTimeoutSeconds = 300;

    /// <summary>
    /// Default circuit breaker failure threshold.
    /// </summary>
    public const int DefaultCircuitBreakerFailureThreshold = 5;

    /// <summary>
    /// Default circuit breaker sampling duration in seconds.
    /// </summary>
    public const int DefaultCircuitBreakerSamplingDurationSeconds = 60;

    /// <summary>
    /// Default circuit breaker break duration in seconds.
    /// </summary>
    public const int DefaultCircuitBreakerBreakDurationSeconds = 30;

    /// <summary>
    /// Default mock balance for offline mode in satoshis.
    /// </summary>
    public const ulong DefaultOfflineMockBalanceSat = 100_000;

    /// <summary>
    /// Payment hash length in hex characters.
    /// </summary>
    public const int PaymentHashLength = 64;

    /// <summary>
    /// Preimage length in hex characters.
    /// </summary>
    public const int PreimageLength = 64;
}
