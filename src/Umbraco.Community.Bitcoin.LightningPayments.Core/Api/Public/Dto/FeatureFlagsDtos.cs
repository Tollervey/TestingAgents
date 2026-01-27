using System.Text.Json.Serialization;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public.Dto;

/// <summary>
/// Response containing the current state of feature flags.
/// </summary>
public record FeatureFlagsResponse
{
    /// <summary>Whether Lightning Payments is globally enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    /// <summary>Whether the paywall feature is enabled.</summary>
    [JsonPropertyName("paywallEnabled")]
    public bool PaywallEnabled { get; init; }

    /// <summary>Whether the tip jar feature is enabled.</summary>
    [JsonPropertyName("tipJarEnabled")]
    public bool TipJarEnabled { get; init; }

    /// <summary>Whether BOLT12 offer support is enabled.</summary>
    [JsonPropertyName("bolt12Enabled")]
    public bool Bolt12Enabled { get; init; }

    /// <summary>Whether notification features are enabled.</summary>
    [JsonPropertyName("notificationsEnabled")]
    public bool NotificationsEnabled { get; init; }

    /// <summary>Whether exchange rate display is enabled.</summary>
    [JsonPropertyName("exchangeRatesEnabled")]
    public bool ExchangeRatesEnabled { get; init; }
}
