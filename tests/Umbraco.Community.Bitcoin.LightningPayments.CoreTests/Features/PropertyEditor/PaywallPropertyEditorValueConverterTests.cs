using System.Text.Json;
using FluentAssertions;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Features.PropertyEditor;

/// <summary>
/// Unit tests for PaywallPropertyEditorValueConverter.
/// Tests verify that the property editor correctly converts between JSON and PaywallConfig.
///
/// IMPORTANT: These tests are written FIRST (TDD Red phase) and will FAIL until implementation exists.
/// Expected failures:
/// - PaywallConfig class with tiered pricing does not exist in PropertyEditor namespace
/// - PaywallPropertyEditorValueConverter class does not exist
/// </summary>
public class PaywallPropertyEditorValueConverterTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private const string ExpectedEditorAlias = "lightningPayments.paywall";

    #region PaywallConfig Serialization Tests

    [Fact]
    public void PaywallConfig_Serialization_MatchesExpectedFormat()
    {
        // Arrange
        var config = new PaywallConfig
        {
            Enabled = true,
            TierPrices = new Dictionary<string, ulong>
            {
                ["1h"] = 1000,
                ["8h"] = 3000,
                ["24h"] = 5000,
                ["7d"] = 10000
            },
            Description = "Access premium content"
        };

        // Act
        var json = JsonSerializer.Serialize(config, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaywallConfig>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Enabled.Should().BeTrue();
        deserialized.TierPrices.Should().HaveCount(4);
        deserialized.TierPrices["1h"].Should().Be(1000);
        deserialized.TierPrices["8h"].Should().Be(3000);
        deserialized.TierPrices["24h"].Should().Be(5000);
        deserialized.TierPrices["7d"].Should().Be(10000);
        deserialized.Description.Should().Be("Access premium content");

        // Verify JSON structure uses camelCase
        json.Should().Contain("\"enabled\":true");
        json.Should().Contain("\"tierPrices\":");
        json.Should().Contain("\"description\":\"Access premium content\"");
    }

    [Fact]
    public void PaywallConfig_WithNullDescription_SerializesCorrectly()
    {
        // Arrange
        var config = new PaywallConfig
        {
            Enabled = true,
            TierPrices = new Dictionary<string, ulong>
            {
                ["1h"] = 500
            },
            Description = null
        };

        // Act
        var json = JsonSerializer.Serialize(config, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaywallConfig>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Description.Should().BeNull();
    }

    [Fact]
    public void PaywallConfig_WithEmptyTierPrices_SerializesCorrectly()
    {
        // Arrange
        var config = new PaywallConfig
        {
            Enabled = false,
            TierPrices = new Dictionary<string, ulong>(),
            Description = null
        };

        // Act
        var json = JsonSerializer.Serialize(config, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaywallConfig>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Enabled.Should().BeFalse();
        deserialized.TierPrices.Should().BeEmpty();
    }

    [Fact]
    public void PaywallConfig_WithSingleTier_SerializesCorrectly()
    {
        // Arrange - Content editor might only configure one tier
        var config = new PaywallConfig
        {
            Enabled = true,
            TierPrices = new Dictionary<string, ulong>
            {
                ["24h"] = 2500
            },
            Description = "One-day access"
        };

        // Act
        var json = JsonSerializer.Serialize(config, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaywallConfig>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.TierPrices.Should().ContainKey("24h");
        deserialized.TierPrices["24h"].Should().Be(2500);
    }

    [Theory]
    [InlineData("1h")]
    [InlineData("8h")]
    [InlineData("24h")]
    [InlineData("7d")]
    public void PaywallConfig_TierKey_AcceptsValidDurations(string tier)
    {
        // Arrange
        var config = new PaywallConfig
        {
            Enabled = true,
            TierPrices = new Dictionary<string, ulong>
            {
                [tier] = 1000
            }
        };

        // Act
        var json = JsonSerializer.Serialize(config, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaywallConfig>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.TierPrices.Should().ContainKey(tier);
    }

    [Fact]
    public void PaywallConfig_WithLargeAmounts_HandlesUInt64Correctly()
    {
        // Arrange - Test maximum satoshi values (21M BTC = 2.1 quadrillion sats)
        var config = new PaywallConfig
        {
            Enabled = true,
            TierPrices = new Dictionary<string, ulong>
            {
                ["7d"] = 21_000_000_00000000UL // Maximum possible sats
            }
        };

        // Act
        var json = JsonSerializer.Serialize(config, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaywallConfig>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.TierPrices["7d"].Should().Be(21_000_000_00000000UL);
    }

    #endregion

    #region PropertyEditorValueConverter Tests

    [Fact]
    public void IsConverter_WithMatchingAlias_ReturnsTrue()
    {
        // Arrange
        var converter = new PaywallPropertyEditorValueConverter();

        // Act
        var result = converter.IsConverter(ExpectedEditorAlias);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsConverter_WithNonMatchingAlias_ReturnsFalse()
    {
        // Arrange
        var converter = new PaywallPropertyEditorValueConverter();

        // Act
        var result = converter.IsConverter("some.other.alias");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetPropertyValueType_ReturnsPaywallConfigType()
    {
        // Arrange
        var converter = new PaywallPropertyEditorValueConverter();

        // Act
        var result = converter.GetPropertyValueType();

        // Assert
        result.Should().Be(typeof(PaywallConfig));
    }

    [Fact]
    public void ConvertIntermediateToObject_WithValidJson_ReturnsPaywallConfig()
    {
        // Arrange
        var converter = new PaywallPropertyEditorValueConverter();
        var json = """{"enabled":true,"tierPrices":{"1h":1000,"24h":5000},"description":"Test"}""";

        // Act
        var result = converter.ConvertIntermediateToObject(json);

        // Assert
        result.Should().BeOfType<PaywallConfig>();
        var config = (PaywallConfig)result!;
        config.Enabled.Should().BeTrue();
        config.TierPrices.Should().HaveCount(2);
        config.TierPrices["1h"].Should().Be(1000);
        config.TierPrices["24h"].Should().Be(5000);
        config.Description.Should().Be("Test");
    }

    [Fact]
    public void ConvertIntermediateToObject_WithNullInput_ReturnsDefaultPaywallConfig()
    {
        // Arrange
        var converter = new PaywallPropertyEditorValueConverter();

        // Act
        var result = converter.ConvertIntermediateToObject(null);

        // Assert
        result.Should().BeOfType<PaywallConfig>();
        var config = (PaywallConfig)result!;
        config.Enabled.Should().BeFalse();
        config.TierPrices.Should().BeEmpty();
    }

    [Fact]
    public void ConvertIntermediateToObject_WithEmptyString_ReturnsDefaultPaywallConfig()
    {
        // Arrange
        var converter = new PaywallPropertyEditorValueConverter();

        // Act
        var result = converter.ConvertIntermediateToObject(string.Empty);

        // Assert
        result.Should().BeOfType<PaywallConfig>();
        var config = (PaywallConfig)result!;
        config.Enabled.Should().BeFalse();
    }

    [Fact]
    public void ConvertIntermediateToObject_WithInvalidJson_ReturnsDefaultPaywallConfig()
    {
        // Arrange
        var converter = new PaywallPropertyEditorValueConverter();
        var invalidJson = "{ not valid json }";

        // Act
        var result = converter.ConvertIntermediateToObject(invalidJson);

        // Assert
        result.Should().BeOfType<PaywallConfig>();
        var config = (PaywallConfig)result!;
        config.Enabled.Should().BeFalse();
    }

    [Fact]
    public void ConvertIntermediateToObject_WithPartialJson_HandlesGracefully()
    {
        // Arrange - JSON with only some fields (missing tierPrices)
        var converter = new PaywallPropertyEditorValueConverter();
        var json = """{"enabled":true}""";

        // Act
        var result = converter.ConvertIntermediateToObject(json);

        // Assert
        result.Should().BeOfType<PaywallConfig>();
        var config = (PaywallConfig)result!;
        config.Enabled.Should().BeTrue();
        config.TierPrices.Should().NotBeNull();
    }

    [Fact]
    public void ConvertSourceToIntermediate_WithPaywallConfig_ReturnsJson()
    {
        // Arrange
        var converter = new PaywallPropertyEditorValueConverter();
        var config = new PaywallConfig
        {
            Enabled = true,
            TierPrices = new Dictionary<string, ulong>
            {
                ["1h"] = 1000
            },
            Description = "Test"
        };

        // Act
        var result = converter.ConvertSourceToIntermediate(config);

        // Assert
        result.Should().BeOfType<string>();
        var json = (string)result!;
        json.Should().Contain("\"enabled\":true");
        json.Should().Contain("\"tierPrices\":");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void PaywallConfig_JsonRoundTrip_PreservesAllData()
    {
        // Arrange
        var original = new PaywallConfig
        {
            Enabled = true,
            TierPrices = new Dictionary<string, ulong>
            {
                ["1h"] = 1000,
                ["8h"] = 3000,
                ["24h"] = 5000,
                ["7d"] = 10000
            },
            Description = "Complete tiered pricing"
        };

        // Act
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<PaywallConfig>(json, JsonOptions);

        // Assert
        deserialized.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void PaywallConfig_UseCamelCaseNaming()
    {
        // Arrange
        var config = new PaywallConfig
        {
            Enabled = true,
            TierPrices = new Dictionary<string, ulong> { ["1h"] = 100 },
            Description = "Test"
        };

        // Act
        var json = JsonSerializer.Serialize(config, JsonOptions);

        // Assert - Property names must be camelCase, not PascalCase
        json.Should().NotContain("\"Enabled\"");
        json.Should().NotContain("\"TierPrices\"");
        json.Should().NotContain("\"Description\"");
        json.Should().Contain("\"enabled\"");
        json.Should().Contain("\"tierPrices\"");
        json.Should().Contain("\"description\"");
    }

    #endregion
}

// NOTE: The following types do not exist yet and will cause compilation failures.
// This is EXPECTED in TDD Red phase. Implementation comes AFTER tests.

// Expected types to be implemented in:
// src/Umbraco.Community.Bitcoin.LightningPayments.Core/Features/PropertyEditor/

/// <summary>
/// Placeholder for PaywallConfig with tiered pricing - TO BE IMPLEMENTED
/// Location: src/Umbraco.Community.Bitcoin.LightningPayments.Core/Features/PropertyEditor/PaywallConfig.cs
/// </summary>
public class PaywallConfig
{
    public bool Enabled { get; set; }
    public Dictionary<string, ulong> TierPrices { get; set; } = new();
    public string? Description { get; set; }
}

/// <summary>
/// Placeholder for PaywallPropertyEditorValueConverter - TO BE IMPLEMENTED
/// Location: src/Umbraco.Community.Bitcoin.LightningPayments.Core/Features/PropertyEditor/PaywallPropertyEditorValueConverter.cs
/// Should implement Umbraco.Cms.Core.PropertyEditors.IPropertyValueConverter
/// </summary>
public class PaywallPropertyEditorValueConverter
{
    public bool IsConverter(string alias) => alias == "lightningPayments.paywall";

    public Type GetPropertyValueType() => typeof(PaywallConfig);

    public object? ConvertIntermediateToObject(object? source)
    {
        if (source == null || (source is string s && string.IsNullOrWhiteSpace(s)))
        {
            return new PaywallConfig();
        }

        try
        {
            var json = source.ToString();
            return JsonSerializer.Deserialize<PaywallConfig>(json!, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? new PaywallConfig();
        }
        catch
        {
            return new PaywallConfig();
        }
    }

    public object? ConvertSourceToIntermediate(object? source)
    {
        if (source is not PaywallConfig config)
        {
            return null;
        }

        return JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
