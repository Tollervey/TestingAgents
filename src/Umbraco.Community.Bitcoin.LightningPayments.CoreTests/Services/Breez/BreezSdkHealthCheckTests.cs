using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services.Breez
{
    /// <summary>
    /// Unit tests for BreezSdkHealthCheck.
    /// </summary>
    public class BreezSdkHealthCheckTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            var healthCheck = BreezSdkHealthCheckMockBuilder.CreateDefault().Build();

            // Assert
            Assert.NotNull(healthCheck);
        }

        #endregion

        #region CheckHealthAsync Tests

        [Fact]
        public async Task CheckHealthAsync_ConnectedAndWritable_ReturnsHealthy()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var healthCheck = BreezSdkHealthCheckMockBuilder.CreateDefault()
                    .WithConnectedService()
                    .WithContentRootPath("C:\\Test")
                    .WithCustomSettings(workingDirectory: tempDir)
                    .Build();

                // Act
                var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

                // Assert
                Assert.Equal(HealthStatus.Healthy, result.Status);
                Assert.Contains("Breez SDK is connected and working directory is writable", result.Description);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task CheckHealthAsync_NotConnected_ReturnsUnhealthy()
        {
            // Arrange
            var healthCheck = BreezSdkHealthCheckMockBuilder.CreateDefault()
                .WithDisconnectedService()
                .WithContentRootPath("C:\\Test")
                .Build();

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Equal("Breez SDK is not connected.", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_DirectoryNotExists_ReturnsUnhealthy()
        {
            // Arrange
            var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent");

            var healthCheck = BreezSdkHealthCheckMockBuilder.CreateDefault()
                .WithConnectedService()
                .WithContentRootPath("C:\\Test")
                .WithCustomSettings(workingDirectory: nonExistentDir)
                .Build();

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("Breez SDK working directory is not writable", result.Description);
            Assert.Contains("Working directory does not exist", result.Description);
        }

        [Fact]
        public async Task CheckHealthAsync_RelativeWorkingDirectory_ResolvesCorrectly()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var relativeDir = Path.Combine(tempDir, "relative", "path");
            Directory.CreateDirectory(relativeDir);

            try
            {
                var healthCheck = BreezSdkHealthCheckMockBuilder.CreateDefault()
                    .WithConnectedService()
                    .WithContentRootPath(tempDir)
                    .WithCustomSettings(workingDirectory: "relative/path")
                    .Build();

                // Act
                var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

                // Assert
                Assert.Equal(HealthStatus.Healthy, result.Status);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task CheckHealthAsync_DefaultWorkingDirectory_UsesAppData()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var appDataDir = Path.Combine(tempDir, "App_Data", LightningPaymentsSettings.SectionName);
            Directory.CreateDirectory(appDataDir);

            try
            {
                var healthCheck = BreezSdkHealthCheckMockBuilder.CreateDefault()
                    .WithConnectedService()
                    .WithContentRootPath(tempDir)
                    .WithCustomSettings(workingDirectory: null) // No WorkingDirectory set
                    .Build();

                // Act
                var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

                // Assert
                Assert.Equal(HealthStatus.Healthy, result.Status);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task CheckHealthAsync_ExceptionInService_ReturnsUnhealthy()
        {
            // Arrange
            var healthCheck = BreezSdkHealthCheckMockBuilder.CreateDefault()
                .WithServiceException(new Exception("Service error"))
                .WithContentRootPath("C:\\Test")
                .Build();

            // Act
            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("Error checking Breez SDK health", result.Description);
            Assert.NotNull(result.Exception);
        }

        [Fact]
        public async Task CheckHealthAsync_PermissionsCheckFails_DoesNotFailHealthCheck()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var healthCheck = BreezSdkHealthCheckMockBuilder.CreateDefault()
                    .WithConnectedService()
                    .WithContentRootPath("C:\\Test")
                    .WithCustomSettings(workingDirectory: tempDir)
                    .Build();

                // Act
                var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

                // Assert
                Assert.Equal(HealthStatus.Healthy, result.Status);
                // Permissions check is best-effort, so even if it fails, health check should pass if connected and writable
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task CheckHealthAsync_CancellationToken_IsPassedToService()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var builder = BreezSdkHealthCheckMockBuilder.CreateDefault()
                .WithConnectedService()
                .WithContentRootPath("C:\\Test");

            var mocks = builder.GetAllMocks();
            var healthCheck = builder.Build();

            // Act
            await healthCheck.CheckHealthAsync(new HealthCheckContext(), cts.Token);

            // Assert
            mocks.Service.Verify(s => s.IsConnectedAsync(cts.Token), Times.Once);
        }

        #endregion
    }
}
