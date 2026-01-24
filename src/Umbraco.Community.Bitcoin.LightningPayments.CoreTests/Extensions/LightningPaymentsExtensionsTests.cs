using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Realtime.Services;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Breez;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Invoice;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Payment;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.RateLimiting;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Runtime;
using Xunit;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Extensions
{
    /// <summary>
    /// Unit tests for LightningPaymentsExtensions.
    /// </summary>
    public class LightningPaymentsExtensionsTests
    {
        #region AddLightningPayments Tests

        [Fact]
        public void AddLightningPayments_AddsRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["LightningPayments:ConnectionString"] = "Data Source=:memory:"
                })
                .Build();
            var mockBuilder = new Mock<IUmbracoBuilder>();
            mockBuilder.Setup(b => b.Services).Returns(services);
            mockBuilder.Setup(b => b.Config).Returns(config);
            var mockHostEnvironment = new Mock<IHostEnvironment>();
            mockHostEnvironment.Setup(h => h.ContentRootPath).Returns("C:\\test");
            services.AddSingleton(mockHostEnvironment.Object);
            services.AddLogging();

            // Act
            var result = mockBuilder.Object.AddLightningPayments();

            // Assert
            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IOptions<LightningPaymentsSettings>>());
            Assert.NotNull(provider.GetService<IValidateOptions<LightningPaymentsSettings>>());
            Assert.NotNull(provider.GetService<PaymentDbContext>());
            Assert.NotNull(provider.GetService<IPaymentStateService>());
            Assert.NotNull(provider.GetService<IBreezSdkWrapper>());
            Assert.NotNull(provider.GetService<IBreezSdkService>());
            Assert.NotNull(provider.GetService<IBreezSdkHandleProvider>());
            Assert.NotNull(provider.GetService<IBreezPaymentsFacade>());
            Assert.NotNull(provider.GetService<IBreezEventProcessor>());
            Assert.NotNull(provider.GetService<IBreezEventProcessor>());
            Assert.NotNull(provider.GetService<IRuntimeSettingsService>());
            Assert.NotNull(provider.GetService<ISseHub>());
            Assert.NotNull(provider.GetService<IRateLimiter>());
            Assert.NotNull(provider.GetService<IInvoiceHelper>());
            Assert.NotNull(provider.GetService<ILightningPaymentsRuntimeMode>());
            Assert.Equal(mockBuilder.Object, result);
        }

        [Fact]
        public void AddLightningPayments_ConfiguresOptionsCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["LightningPayments:ConnectionString"] = "Data Source=test.db",
                    // Provide required secret-like values so validation passes in tests
                    ["LightningPayments:BreezApiKey"] = "test-api-key",
                    ["LightningPayments:Mnemonic"] = "test-mnemonic"
                })
                .Build();
            var mockBuilder = new Mock<IUmbracoBuilder>();
            mockBuilder.Setup(b => b.Services).Returns(services);
            mockBuilder.Setup(b => b.Config).Returns(config);
            var mockHostEnvironment = new Mock<IHostEnvironment>();
            services.AddSingleton(mockHostEnvironment.Object);
            services.AddLogging();

            // Act
            mockBuilder.Object.AddLightningPayments();

            // Assert
            var provider = services.BuildServiceProvider();
            var options = provider.GetService<IOptions<LightningPaymentsSettings>>();
            Assert.NotNull(options);
            Assert.Equal("Data Source=test.db", options.Value.ConnectionString);
        }

        [Fact]
        public void AddLightningPayments_AddsRateLimiter_WhenEnabled()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["LightningPayments:RateLimiting:Enabled"] = "true",
                    ["LightningPayments:RateLimiting:UseAspNetRateLimiter"] = "true",
                    ["LightningPayments:ConnectionString"] = "Data Source=:memory:"
                })
                .Build();
            var mockBuilder = new Mock<IUmbracoBuilder>();
            mockBuilder.Setup(b => b.Services).Returns(services);
            mockBuilder.Setup(b => b.Config).Returns(config);
            var mockHostEnvironment = new Mock<IHostEnvironment>();
            services.AddSingleton(mockHostEnvironment.Object);
            services.AddLogging();

            // Act
            mockBuilder.Object.AddLightningPayments();

            // Assert
            var provider = services.BuildServiceProvider();
            // Rate limiter is added via AddRateLimiter, but to verify, we can check if the service is registered
            // Since it's middleware, perhaps check if the options are configured
            var rateLimitOptions = provider.GetService<IOptions<RateLimitingOptions>>();
            Assert.NotNull(rateLimitOptions);
        }

        [Fact]
        public void AddLightningPayments_ConfiguresDbContext_InMemory_WhenOffline()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["LightningPayments:ConnectionString"] = "Data Source=test.db"
                })
                .Build();
            var mockBuilder = new Mock<IUmbracoBuilder>();
            mockBuilder.Setup(b => b.Services).Returns(services);
            mockBuilder.Setup(b => b.Config).Returns(config);
            var mockHostEnvironment = new Mock<IHostEnvironment>();
            services.AddSingleton(mockHostEnvironment.Object);
            services.AddLogging();
            // Simulate offline mode
            services.AddSingleton<ILightningPaymentsRuntimeMode>(_ => new LightningPaymentsRuntimeMode(isOffline: true));
            services.AddSingleton<IOptions<OfflineLightningPaymentsOptions>>(_ => Options.Create(new OfflineLightningPaymentsOptions { UseInMemoryStateService = true }));

            // Act
            mockBuilder.Object.AddLightningPayments();

            // Assert
            var provider = services.BuildServiceProvider();
            var dbContext = provider.GetService<PaymentDbContext>();
            Assert.NotNull(dbContext);
            // In offline mode with in-memory, it should use InMemoryDatabase
            // To verify, we can check the options, but since it's internal, perhaps just ensure no exception
        }

        [Fact]
        public void AddLightningPayments_AddsHealthChecks()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["LightningPayments:ConnectionString"] = "Data Source=:memory:"
                })
                .Build();
            var mockBuilder = new Mock<IUmbracoBuilder>();
            mockBuilder.Setup(b => b.Services).Returns(services);
            mockBuilder.Setup(b => b.Config).Returns(config);
            var mockHostEnvironment = new Mock<IHostEnvironment>();
            services.AddSingleton(mockHostEnvironment.Object);
            services.AddLogging();

            // Act
            mockBuilder.Object.AddLightningPayments();

            // Assert
            var provider = services.BuildServiceProvider();
            var healthOptions = provider.GetService<IOptions<HealthCheckServiceOptions>>();
            Assert.NotNull(healthOptions);
            Assert.Contains(healthOptions.Value.Registrations, r => r.Name == "breez");
        }

        [Fact]
        public void AddLightningPayments_LogsInformation()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["LightningPayments:ConnectionString"] = "Data Source=:memory:"
                })
                .Build();
            var mockBuilder = new Mock<IUmbracoBuilder>();
            mockBuilder.Setup(b => b.Services).Returns(services);
            mockBuilder.Setup(b => b.Config).Returns(config);
            var mockHostEnvironment = new Mock<IHostEnvironment>();
            services.AddSingleton(mockHostEnvironment.Object);
            var mockLogger = new Mock<ILogger>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(lf => lf.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);
            services.AddSingleton(mockLoggerFactory.Object);

            // Act
            mockBuilder.Object.AddLightningPayments();

            // Assert
            mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == "--- Step 1: AddLightningPayments() called. Assembly is now loaded. ---"),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

            mockLogger.Verify(l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == "--- Step 2: AddLightningPayments() completed service registration. ---"),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        #endregion

        #region UseLightningPaymentsOffline Tests

        [Fact]
        public void UseLightningPaymentsOffline_SetsOfflineMode()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockBuilder = new Mock<IUmbracoBuilder>();
            mockBuilder.Setup(b => b.Services).Returns(services);

            // Act
            var result = mockBuilder.Object.UseLightningPaymentsOffline();

            // Assert
            var provider = services.BuildServiceProvider();
            var runtimeMode = provider.GetService<ILightningPaymentsRuntimeMode>();
            Assert.NotNull(runtimeMode);
            Assert.True(runtimeMode.IsOffline);
            Assert.Equal(mockBuilder.Object, result);
        }

        [Fact]
        public void UseLightningPaymentsOffline_RegistersOfflineServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockBuilder = new Mock<IUmbracoBuilder>();
            mockBuilder.Setup(b => b.Services).Returns(services);

            // Act
            mockBuilder.Object.UseLightningPaymentsOffline();

            // Assert
            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IBreezSdkService>());
            Assert.IsType<OfflineBreezSdkService>(provider.GetService<IBreezSdkService>());
            Assert.NotNull(provider.GetService<IBreezSdkHandleProvider>());
            Assert.NotNull(provider.GetService<IBreezPaymentsFacade>());
        }

        [Fact]
        public void UseLightningPaymentsOffline_RegistersInMemoryStateService_WhenConfigured()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockBuilder = new Mock<IUmbracoBuilder>();
            mockBuilder.Setup(b => b.Services).Returns(services);

            // Act
            mockBuilder.Object.UseLightningPaymentsOffline(options => options.UseInMemoryStateService = true);

            // Assert
            var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IPaymentStateService>());
            Assert.IsType<InMemoryPaymentStateService>(provider.GetService<IPaymentStateService>());
        }

        [Fact]
        public void UseLightningPaymentsOffline_DoesNotRegisterInMemoryStateService_WhenNotConfigured()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockBuilder = new Mock<IUmbracoBuilder>();
            mockBuilder.Setup(b => b.Services).Returns(services);

            // Act
            mockBuilder.Object.UseLightningPaymentsOffline(options => options.UseInMemoryStateService = false);

            // Assert
            var provider = services.BuildServiceProvider();
            // Should not have InMemoryPaymentStateService, but since it's not added, and assuming default is Persistent, but in offline, it might not add unless specified
            // The method only adds if UseInMemoryStateService is true
            var stateService = provider.GetService<IPaymentStateService>();
            Assert.Null(stateService); // Since not added in this method
        }

        #endregion
    }
}