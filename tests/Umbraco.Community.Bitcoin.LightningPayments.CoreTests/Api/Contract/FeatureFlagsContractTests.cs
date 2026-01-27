using System.Text.Json;
using FluentAssertions;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public.Dto;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Api.Contract;

public class FeatureFlagsContractTests
{
    [Fact]
    public void FeatureFlagsResponse_JsonPropertyNames_AreCamelCase()
    {
        var response = new FeatureFlagsResponse
        {
            Enabled = true,
            PaywallEnabled = true,
            TipJarEnabled = false,
            Bolt12Enabled = true,
            NotificationsEnabled = false,
            ExchangeRatesEnabled = true
        };

        var json = JsonSerializer.Serialize(response);
        json.Should().Contain("\"enabled\"");
        json.Should().Contain("\"paywallEnabled\"");
        json.Should().Contain("\"tipJarEnabled\"");
        json.Should().Contain("\"bolt12Enabled\"");
        json.Should().Contain("\"notificationsEnabled\"");
        json.Should().Contain("\"exchangeRatesEnabled\"");
    }

    [Fact]
    public void FeatureFlagsResponse_Deserialization_Roundtrip()
    {
        var original = new FeatureFlagsResponse
        {
            Enabled = true,
            PaywallEnabled = true,
            TipJarEnabled = false,
            Bolt12Enabled = true,
            NotificationsEnabled = true,
            ExchangeRatesEnabled = false
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<FeatureFlagsResponse>(json);

        deserialized.Should().BeEquivalentTo(original);
    }
}
