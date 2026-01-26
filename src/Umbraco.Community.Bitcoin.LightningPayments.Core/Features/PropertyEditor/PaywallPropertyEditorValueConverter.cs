using System.Text.Json;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Features.PropertyEditor;

/// <summary>
/// Converts the paywall property editor value between JSON and PaywallConfig.
/// Implements IPropertyValueConverter for Umbraco property editor integration.
/// </summary>
public class PaywallPropertyEditorValueConverter : IPropertyValueConverter
{
    /// <summary>
    /// The property editor alias for the paywall property editor.
    /// </summary>
    public const string EditorAlias = "lightningPayments.paywall";

    private readonly ILogger<PaywallPropertyEditorValueConverter> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public PaywallPropertyEditorValueConverter(ILogger<PaywallPropertyEditorValueConverter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Determines if this converter handles the specified property type.
    /// </summary>
    public bool IsConverter(IPublishedPropertyType propertyType)
    {
        return propertyType.EditorAlias.Equals(EditorAlias, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the type that this converter produces.
    /// </summary>
    public Type GetPropertyValueType(IPublishedPropertyType propertyType)
    {
        return typeof(PaywallConfig);
    }

    /// <summary>
    /// Gets the level at which the value varies.
    /// </summary>
    public PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType)
    {
        return PropertyCacheLevel.Element;
    }

    /// <summary>
    /// Converts the source value (from database) to an intermediate value.
    /// </summary>
    public object? ConvertSourceToIntermediate(
        IPublishedElement owner,
        IPublishedPropertyType propertyType,
        object? source,
        bool preview)
    {
        // Source is typically already a string from the database
        return source;
    }

    /// <summary>
    /// Converts the intermediate value to the final object type (PaywallConfig).
    /// </summary>
    public object? ConvertIntermediateToObject(
        IPublishedElement owner,
        IPublishedPropertyType propertyType,
        PropertyCacheLevel referenceCacheLevel,
        object? inter,
        bool preview)
    {
        if (inter == null)
        {
            return new PaywallConfig();
        }

        var json = inter.ToString();
        if (string.IsNullOrWhiteSpace(json))
        {
            return new PaywallConfig();
        }

        try
        {
            var config = JsonSerializer.Deserialize<PaywallConfig>(json, JsonOptions);
            return config ?? new PaywallConfig();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize PaywallConfig from JSON: {Json}", json);
            return new PaywallConfig();
        }
    }

    /// <summary>
    /// Converts the intermediate value to XPath (for XPath queries).
    /// </summary>
    public object? ConvertIntermediateToXPath(
        IPublishedElement owner,
        IPublishedPropertyType propertyType,
        PropertyCacheLevel referenceCacheLevel,
        object? inter,
        bool preview)
    {
        // Not supporting XPath for this complex type
        return null;
    }

    /// <summary>
    /// Determines if a value is a value (i.e., not null/empty).
    /// </summary>
    public bool? IsValue(object? value, PropertyValueLevel level)
    {
        return level switch
        {
            PropertyValueLevel.Source => value is string s && !string.IsNullOrWhiteSpace(s),
            PropertyValueLevel.Inter => value is string s && !string.IsNullOrWhiteSpace(s),
            PropertyValueLevel.Object => value is PaywallConfig config && config.Enabled,
            _ => null
        };
    }
}
