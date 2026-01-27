using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public.Dto;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Runtime;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Api;

public class FeatureFlagsControllerTests
{
    private readonly Mock<IRuntimeSettingsService> _runtimeSettingsMock = new();
    private readonly Mock<ILogger<FeatureFlagsController>> _loggerMock = new();

    private FeatureFlagsController CreateController(
        NotificationOptions? notifOptions = null,
        ExchangeRateOptions? exchangeOptions = null)
    {
        return new FeatureFlagsController(
            _runtimeSettingsMock.Object,
            _loggerMock.Object,
            Options.Create(notifOptions ?? new NotificationOptions()),
            Options.Create(exchangeOptions ?? new ExchangeRateOptions()));
    }

    [Fact]
    public async Task GetFeatureFlags_ReturnsOk_WithFlags()
    {
        _runtimeSettingsMock.Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RuntimeFeatureFlags { Enabled = true, PaywallEnabled = true, TipJarEnabled = true });

        var controller = CreateController();
        var result = await controller.GetFeatureFlags(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var flags = okResult.Value.Should().BeOfType<FeatureFlagsResponse>().Subject;
        flags.Enabled.Should().BeTrue();
        flags.PaywallEnabled.Should().BeTrue();
        flags.TipJarEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetFeatureFlags_ReflectsDisabledState()
    {
        _runtimeSettingsMock.Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RuntimeFeatureFlags { Enabled = false, PaywallEnabled = false, TipJarEnabled = false });

        var controller = CreateController();
        var result = await controller.GetFeatureFlags(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var flags = okResult.Value.Should().BeOfType<FeatureFlagsResponse>().Subject;
        flags.Enabled.Should().BeFalse();
        flags.PaywallEnabled.Should().BeFalse();
        flags.TipJarEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetFeatureFlags_NotificationsEnabled_WhenConfigured()
    {
        _runtimeSettingsMock.Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RuntimeFeatureFlags { Enabled = true });

        var notifOptions = new NotificationOptions { Enabled = true };
        var controller = CreateController(notifOptions);
        var result = await controller.GetFeatureFlags(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var flags = okResult.Value.Should().BeOfType<FeatureFlagsResponse>().Subject;
        flags.NotificationsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetFeatureFlags_ExchangeRatesEnabled_WhenConfigured()
    {
        _runtimeSettingsMock.Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RuntimeFeatureFlags { Enabled = true });

        var exchangeOptions = new ExchangeRateOptions { Enabled = true };
        var controller = CreateController(exchangeOptions: exchangeOptions);
        var result = await controller.GetFeatureFlags(CancellationToken.None);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var flags = okResult.Value.Should().BeOfType<FeatureFlagsResponse>().Subject;
        flags.ExchangeRatesEnabled.Should().BeTrue();
    }
}
