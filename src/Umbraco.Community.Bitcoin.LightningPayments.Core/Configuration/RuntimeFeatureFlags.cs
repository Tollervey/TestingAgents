using System.Text.Json.Serialization;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration
{
    public class RuntimeFeatureFlags
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("hideUiWhenDisabled")]
        public bool HideUiWhenDisabled { get; set; } = true;

        [JsonPropertyName("tipJarEnabled")]
        public bool TipJarEnabled { get; set; } = true;

        [JsonPropertyName("paywallEnabled")]
        public bool PaywallEnabled { get; set; } = true;
    }
}

