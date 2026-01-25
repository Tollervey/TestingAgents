using System.ComponentModel.DataAnnotations;

namespace Breez.Sdk.Liquid.Extensions.Core.Configuration;

/// <summary>
/// Configuration options for BreezSDK.
/// </summary>
public class BreezSdkOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "BreezSdk";

    /// <summary>
    /// Breez API key for authentication.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// BIP39 mnemonic for wallet access.
    /// </summary>
    public string? Mnemonic { get; set; }

    /// <summary>
    /// Target network (Mainnet, Testnet, Regtest).
    /// </summary>
    public BreezNetwork Network { get; set; } = BreezNetwork.Mainnet;

    /// <summary>
    /// Working directory for SDK data files.
    /// Defaults to a "breez-data" subdirectory in the user's local application data folder.
    /// </summary>
    public string? WorkingDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "breez-data");

    /// <summary>
    /// Webhook URL for payment notifications.
    /// </summary>
    [Url]
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Shared secret for webhook HMAC validation.
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Maximum invoice amount in satoshis.
    /// </summary>
    [Range(1, long.MaxValue)]
    public ulong MaxInvoiceAmountSat { get; set; } = 10_000_000;

    /// <summary>
    /// Maximum invoice description length.
    /// </summary>
    [Range(1, 1000)]
    public int MaxInvoiceDescriptionLength { get; set; } = 200;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    [Range(5, 300)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable offline/mock mode for development.
    /// </summary>
    public bool OfflineMode { get; set; }

    /// <summary>
    /// Circuit breaker configuration.
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Configuration options for automatic reconnection behavior.
    /// When null, default reconnection options are used.
    /// </summary>
    public ReconnectionOptions? Reconnection { get; set; }

    // Offline mode simulation properties

    /// <summary>
    /// Simulated delay in milliseconds for offline mode operations.
    /// </summary>
    public int OfflineSimulateDelayMs { get; set; } = 0;

    /// <summary>
    /// Simulated failure rate (0.0 to 1.0) for offline mode operations.
    /// </summary>
    [Range(0.0, 1.0)]
    public double OfflineSimulateFailureRate { get; set; } = 0.0;

    /// <summary>
    /// Mock wallet balance for offline mode in satoshis.
    /// </summary>
    public ulong OfflineMockBalanceSat { get; set; } = 100_000;
}
