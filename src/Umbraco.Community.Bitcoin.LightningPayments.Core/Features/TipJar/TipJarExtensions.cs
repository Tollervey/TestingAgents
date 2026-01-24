using Umbraco.Community.Bitcoin.LightningPayments.Core.Features.TipJar.Models;
using System.Text.Json;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Features.TipJar
{
    /// <summary>
    /// Extension methods for working with TipJar properties.
    /// </summary>
    public static class TipJarExtensions
    {
        /// <summary>
        /// Gets the TipJar configuration from a property value.
        /// Converts the JSON string to a strongly-typed TipJarConfig object.
        /// </summary>
        /// <param name="content">The published content</param>
        /// <param name="propertyAlias">The property alias (default: "tipJar")</param>
        /// <returns>TipJarConfig object or null if not found/invalid</returns>
        public static TipJarConfig? GetTipJar(this IPublishedContent content, string propertyAlias = "tipJar")
        {
            var value = content.Value<string>(propertyAlias);

            if (string.IsNullOrWhiteSpace(value))
                return null;

            try
            {
                return JsonSerializer.Deserialize<TipJarConfig>(value, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }
    }
}

