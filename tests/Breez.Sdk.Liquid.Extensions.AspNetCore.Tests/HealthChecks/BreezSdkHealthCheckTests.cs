using Breez.Sdk.Liquid.Extensions.AspNetCore.HealthChecks;
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;

namespace Breez.Sdk.Liquid.Extensions.AspNetCore.Tests.HealthChecks;

/// <summary>
/// Unit tests for BreezSdkHealthCheck.
/// Tests follow TDD RED-GREEN-REFACTOR pattern.
/// These tests are written FIRST and will FAIL until the implementation is created.
/// </summary>
public class BreezSdkHealthCheckTests
{
    private readonly Mock<IBreezHealthCheck> _mockHealthCheck;

    public BreezSdkHealthCheckTests()
    {
        _mockHealthCheck = new Mock<IBreezHealthCheck>();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSdkIsHealthyAndConnected_ReturnsHealthy()
    {
        // Arrange
        _mockHealthCheck
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(BreezHealthStatus.Healthy("SDK connected", TimeSpan.FromMilliseconds(50)));

        var healthCheck = new BreezSdkHealthCheck(_mockHealthCheck.Object);
        var context = CreateHealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("SDK connected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSdkIsDegraded_ReturnsDegraded()
    {
        // Arrange
        _mockHealthCheck
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(BreezHealthStatus.Degraded("High latency detected", TimeSpan.FromMilliseconds(2000)));

        var healthCheck = new BreezSdkHealthCheck(_mockHealthCheck.Object);
        var context = CreateHealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("High latency detected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSdkIsUnhealthy_ReturnsUnhealthy()
    {
        // Arrange
        _mockHealthCheck
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(BreezHealthStatus.Unhealthy("SDK not connected", TimeSpan.FromMilliseconds(100)));

        var healthCheck = new BreezSdkHealthCheck(_mockHealthCheck.Object);
        var context = CreateHealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("SDK not connected");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthCheckThrows_ReturnsUnhealthy()
    {
        // Arrange
        var expectedException = new InvalidOperationException("SDK initialization failed");
        _mockHealthCheck
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var healthCheck = new BreezSdkHealthCheck(_mockHealthCheck.Object);
        var context = CreateHealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().Be(expectedException);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _mockHealthCheck
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) =>
            {
                await Task.Delay(1000, ct);
                return BreezHealthStatus.Healthy();
            });

        var healthCheck = new BreezSdkHealthCheck(_mockHealthCheck.Object);
        var context = CreateHealthCheckContext();

        // Act
        cts.Cancel();
        var act = async () => await healthCheck.CheckHealthAsync(context, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesDurationInData()
    {
        // Arrange
        var expectedDuration = TimeSpan.FromMilliseconds(150);
        _mockHealthCheck
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(BreezHealthStatus.Healthy("OK", expectedDuration));

        var healthCheck = new BreezSdkHealthCheck(_mockHealthCheck.Object);
        var context = CreateHealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Data.Should().ContainKey("duration_ms");
        result.Data["duration_ms"].Should().Be(expectedDuration.TotalMilliseconds);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHealthCheckHasAdditionalData_IncludesInResult()
    {
        // Arrange
        var additionalData = new Dictionary<string, object>
        {
            ["network"] = "mainnet",
            ["balance_sat"] = 50000UL
        };
        var status = new BreezHealthStatus
        {
            IsHealthy = true,
            IsConnected = true,
            Message = "SDK connected",
            Data = additionalData,
            Duration = TimeSpan.FromMilliseconds(50)
        };

        _mockHealthCheck
            .Setup(x => x.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        var healthCheck = new BreezSdkHealthCheck(_mockHealthCheck.Object);
        var context = CreateHealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Data.Should().ContainKey("network");
        result.Data["network"].Should().Be("mainnet");
    }

    private static HealthCheckContext CreateHealthCheckContext()
    {
        return new HealthCheckContext
        {
            Registration = new HealthCheckRegistration(
                "breez-sdk",
                instance: Mock.Of<IHealthCheck>(),
                failureStatus: HealthStatus.Unhealthy,
                tags: null)
        };
    }
}
