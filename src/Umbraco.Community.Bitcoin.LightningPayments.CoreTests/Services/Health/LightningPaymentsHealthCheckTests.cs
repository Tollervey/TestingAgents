using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services.Health
{
    /// <summary>
    /// Unit tests for LightningPaymentsHealthCheck.
    /// </summary>
    public class LightningPaymentsHealthCheckTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var healthCheck = LightningPaymentsHealthCheckMockBuilder.CreateDefault().Build();

            // Assert
            Assert.NotNull(healthCheck);
        }

        #endregion

        #region CheckHealthAsync Tests

        [Fact]
        public async Task CheckHealthAsync_ServiceHealthy_ReturnsHealthy()
        {
            // Arrange
            var healthCheck = LightningPaymentsHealthCheckMockBuilder.CreateDefault()
                .WithHealthyService()
                .Build();

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Equal("Lightning payments service is operational.", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_ServiceUnhealthy_ReturnsUnhealthy()
        {
            // Arrange
            var healthCheck = LightningPaymentsHealthCheckMockBuilder.CreateDefault()
                .WithUnhealthyService()
                .Build();

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Equal("Lightning payments service is not responding.", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_ServiceThrowsException_ReturnsUnhealthy()
        {
            // Arrange
            var exception = new Exception("Service error");
            var healthCheck = LightningPaymentsHealthCheckMockBuilder.CreateDefault()
                .WithServiceException(exception)
                .Build();

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Equal("Error checking lightning payments health.", result.Description);
            Assert.Equal(exception, result.Exception);
        }

        [Fact]
        public async Task CheckHealthAsync_CancellationToken_IsPassedToService()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var builder = LightningPaymentsHealthCheckMockBuilder.CreateDefault()
                .WithHealthyService();

            var mocks = builder.GetAllMocks();
            var healthCheck = builder.Build();

            // Act
            await healthCheck.CheckHealthAsync(new HealthCheckContext(), cts.Token);

            // Assert
            mocks.Verify(s => s.IsServiceHealthyAsync(), Times.Once);
        }

        #endregion
    }
}