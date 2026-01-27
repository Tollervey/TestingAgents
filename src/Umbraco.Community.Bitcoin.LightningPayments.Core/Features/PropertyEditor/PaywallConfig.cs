using System.Text.Json.Serialization;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Features.PropertyEditor;

/// <summary>
/// Configuration for enabling a paywall with tiered pricing.
/// Used by the property editor to store paywall settings on content.
/// </summary>
public class PaywallConfig
{
    /// <summary>
    /// Whether the paywall is enabled for the content.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Tiered pricing configuration mapping duration keys to satoshi amounts.
    /// Valid keys: "1h", "8h", "24h", "7d"
    /// </summary>
    /// <example>
    /// { "1h": 1000, "8h": 3000, "24h": 5000, "7d": 10000 }
    /// </example>
    [JsonPropertyName("tierPrices")]
    public Dictionary<string, ulong> TierPrices { get; set; } = new();

    /// <summary>
    /// Optional description displayed to users before payment.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets the satoshi amount for a specific tier.
    /// </summary>
    /// <param name="tier">Tier key (1h, 8h, 24h, 7d)</param>
    /// <returns>Amount in satoshis, or null if tier not configured</returns>
    public ulong? GetTierPrice(string tier)
    {
        return TierPrices.TryGetValue(tier, out var price) ? price : null;
    }

    /// <summary>
    /// Validates that tier keys are valid duration formats.
    /// </summary>
    public static readonly HashSet<string> ValidTierKeys = new() { "1h", "8h", "24h", "7d" };

    /// <summary>
    /// Checks if all tier keys are valid.
    /// </summary>
    public bool HasValidTierKeys()
    {
        return TierPrices.Keys.All(key => ValidTierKeys.Contains(key));
    }

    /// <summary>
    /// Gets the TimeSpan duration for a tier key.
    /// </summary>
    /// <param name="tier">Tier key (1h, 8h, 24h, 7d)</param>
    /// <returns>TimeSpan for the tier, or null if invalid</returns>
    public static TimeSpan? GetTierDuration(string tier)
    {
        return tier switch
        {
            "1h" => TimeSpan.FromHours(1),
            "8h" => TimeSpan.FromHours(8),
            "24h" => TimeSpan.FromHours(24),
            "7d" => TimeSpan.FromDays(7),
            _ => null
        };
    }
}
