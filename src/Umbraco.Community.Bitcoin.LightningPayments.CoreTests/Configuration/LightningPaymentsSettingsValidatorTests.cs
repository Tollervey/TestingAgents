using System;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Configuration
{
    public class LightningPaymentsSettingsValidatorTests
    {
        private const string BreezKeyName = "LightningPayments__BreezApiKey";
        private const string MnemonicKeyName = "LightningPayments__Mnemonic";
        private const string WebhookKeyName = "LightningPayments__WebhookSecret";

        [Fact]
        public void Validate_ReturnsSuccess_When_NotProduction_And_NoSecrets()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<LightningPaymentsSettingsValidator>>();
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.SetupGet(e => e.EnvironmentName).Returns(Environments.Development);

            var options = new LightningPaymentsSettings
            {
                BreezApiKey = null,
                Mnemonic = null,
                WebhookSecret = null
            };

            var validator = new LightningPaymentsSettingsValidator(mockLogger.Object, mockEnv.Object);

            // Act
            var result = validator.Validate(name: null, options: options);

            // Assert
            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Validate_Fails_InProduction_When_SecretsInConfig_And_NoEnvVars()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<LightningPaymentsSettingsValidator>>();
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);

            // Ensure environment variables are absent for the test
            var previousBreez = Environment.GetEnvironmentVariable(BreezKeyName);
            var previousMnemonic = Environment.GetEnvironmentVariable(MnemonicKeyName);
            var previousWebhook = Environment.GetEnvironmentVariable(WebhookKeyName);
            try
            {
                Environment.SetEnvironmentVariable(BreezKeyName, null);
                Environment.SetEnvironmentVariable(MnemonicKeyName, null);
                Environment.SetEnvironmentVariable(WebhookKeyName, null);

                var options = new LightningPaymentsSettings
                {
                    BreezApiKey = "secret-breez",
                    Mnemonic = "secret-mnemonic",
                    WebhookSecret = "secret-webhook"
                };

                var validator = new LightningPaymentsSettingsValidator(mockLogger.Object, mockEnv.Object);

                // Act
                var result = validator.Validate(name: null, options: options);

                // Assert
                Assert.False(result.Succeeded);

                // Verify a critical log was written containing the expected message
                mockLogger.Verify(
                    x => x.Log(
                        LogLevel.Critical,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v != null && v.ToString().Contains("Detected secret-like configuration values")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.AtLeastOnce);
            }
            finally
            {
                // Restore environment
                Environment.SetEnvironmentVariable(BreezKeyName, previousBreez);
                Environment.SetEnvironmentVariable(MnemonicKeyName, previousMnemonic);
                Environment.SetEnvironmentVariable(WebhookKeyName, previousWebhook);
            }
        }

        [Fact]
        public void Validate_Succeeds_InProduction_When_SecretsProvidedViaEnvVars()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<LightningPaymentsSettingsValidator>>();
            var mockEnv = new Mock<IHostEnvironment>();
            mockEnv.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);

            // Save & set environment variables for the test
            var previousBreez = Environment.GetEnvironmentVariable(BreezKeyName);
            var previousMnemonic = Environment.GetEnvironmentVariable(MnemonicKeyName);
            var previousWebhook = Environment.GetEnvironmentVariable(WebhookKeyName);
            try
            {
                Environment.SetEnvironmentVariable(BreezKeyName, "env-breez");
                Environment.SetEnvironmentVariable(MnemonicKeyName, "env-mnemonic");
                Environment.SetEnvironmentVariable(WebhookKeyName, "env-webhook");

                var options = new LightningPaymentsSettings
                {
                    BreezApiKey = "secret-breez",
                    Mnemonic = "secret-mnemonic",
                    WebhookSecret = "secret-webhook"
                };

                var validator = new LightningPaymentsSettingsValidator(mockLogger.Object, mockEnv.Object);

                // Act
                var result = validator.Validate(name: null, options: options);

                // Assert
                Assert.True(result.Succeeded);
            }
            finally
            {
                // Restore environment
                Environment.SetEnvironmentVariable(BreezKeyName, previousBreez);
                Environment.SetEnvironmentVariable(MnemonicKeyName, previousMnemonic);
                Environment.SetEnvironmentVariable(WebhookKeyName, previousWebhook);
            }
        }
    }
}