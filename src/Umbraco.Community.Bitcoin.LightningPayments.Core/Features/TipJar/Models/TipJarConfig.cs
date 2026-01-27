using System.Text.Json.Serialization;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Features.TipJar.Models
{
    /// <summary>
    /// Configuration for enabling a tip jar and specifying display currency.
    /// </summary>
    public class TipJarConfig
    {
        /// <summary>
        /// Whether the tip jar is enabled for the content.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// The currency to display in the payment modal for fiat conversion.
        /// </summary>
        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        /// <summary>
        /// The default amounts in satoshis for the tip jar.
        /// Used for rendering preset amount buttons in the UI.
        /// </summary>
        [JsonIgnore]
        public ulong[] DefaultAmounts { get; set; } = [500, 1000, 2500];

        /// <summary>
        /// The label for the tip jar button.
        /// </summary>
        [JsonIgnore]
        public string Label { get; set; } = "âš¡ Tip the Author";
    }
}
