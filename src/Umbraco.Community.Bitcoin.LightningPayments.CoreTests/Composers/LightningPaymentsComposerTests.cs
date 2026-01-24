using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Xunit;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Composers;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Features.Paywall.Services;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Composers
{
    public class LightningPaymentsComposerTests
    {
        [Fact]
        public async Task Compose_Registers_PaywallMessageService_And_HealthCheck()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockBuilder = new Mock<IUmbracoBuilder>();
            mockBuilder.Setup(b => b.Services).Returns(services);

            // Provide a non-null Components() so composer calls to Append() won't hit null
            //var mockComposers = new Mock<IComposerCollection>();
           // mockBuilder.Setup(b => b.Components()).Returns(mockComposers.Object);

            // Provide a minimal IConfiguration so builder.Config is not null during registration.
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Keep section present but empty; rate limiting defaults will be used.
                    // Optionally include keys here if a specific behavior is required by tests.
                })
                .Build();
            mockBuilder.Setup(b => b.Config).Returns(config);

            var composer = new LightningPaymentsComposer();

            // Act
            composer.Compose(mockBuilder.Object);

            // Build provider to inspect registrations
            var provider = services.BuildServiceProvider();

            // Assert - IPaywallMessageService registered as singleton and resolves
            var paywall = provider.GetService<IPaywallMessageService>();
            Assert.NotNull(paywall);
            Assert.IsType<PaywallMessageService>(paywall);

            // Assert - Health check registration exists with expected name
            var healthService = provider.GetService<HealthCheckService>();
            Assert.NotNull(healthService);

            // Use async test pattern instead of blocking.
            var healthReport = await healthService.CheckHealthAsync();
            var hasLightningCheck = healthReport.Entries.Any(e => e.Key == "Lightning Payments");
            Assert.True(hasLightningCheck, "Expected a health check entry named 'Lightning Payments'.");
        }

        [Fact]
        public void Compose_Configures_RateLimiter_RejectionStatusCode()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockBuilder = new Mock<IUmbracoBuilder>();
            mockBuilder.Setup(b => b.Services).Returns(services);

            // Provide a non-null Components() so composer calls to Append() won't hit null
            //var mockComposers = new Mock<IComposerCollection>();
            //mockBuilder.Setup(b => b.Components()).Returns(mockComposers.Object);

            // Provide configuration including the rate limiting section so AddLightningPayments can read it.
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["LightningPayments:RateLimiting:RejectionStatusCode"] = "429"
                })
                .Build();
            mockBuilder.Setup(b => b.Config).Returns(config);

            var composer = new LightningPaymentsComposer();

            // Act
            composer.Compose(mockBuilder.Object);

            // Build provider to inspect configured options
            var provider = services.BuildServiceProvider();

            var rateLimiterOptions = provider.GetService<IOptions<RateLimiterOptions>>();
            Assert.NotNull(rateLimiterOptions);

            // The composer sets RejectionStatusCode = 429
            Assert.Equal(429, rateLimiterOptions.Value.RejectionStatusCode);
        }
    }
}