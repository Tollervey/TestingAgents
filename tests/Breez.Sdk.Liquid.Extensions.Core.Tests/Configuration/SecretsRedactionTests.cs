using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using FluentAssertions;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Configuration;

/// <summary>
/// Unit tests for SecretsRedactor utility class.
/// Tests follow TDD RED phase - these will fail until SecretsRedactor is implemented.
/// </summary>
public class SecretsRedactionTests
{
    #region API Key Redaction Tests

    [Fact]
    public void RedactApiKey_WithValidApiKey_ShowsFirstFourCharsAndAsterisks()
    {
        // Arrange
        var apiKey = "test-api-key-12345678";

        // Act
        var redacted = SecretsRedactor.RedactApiKey(apiKey);

        // Assert
        redacted.Should().StartWith("test");
        redacted.Should().Contain("****");
        redacted.Should().NotContain("12345678");
        redacted.Length.Should().BeGreaterThanOrEqualTo(apiKey.Length);
    }

    [Fact]
    public void RedactApiKey_WithShortApiKey_ShowsFirstFourCharsIfAvailable()
    {
        // Arrange
        var apiKey = "abc1";

        // Act
        var redacted = SecretsRedactor.RedactApiKey(apiKey);

        // Assert
        redacted.Should().Be("abc1****");
    }

    [Fact]
    public void RedactApiKey_WithApiKeyShorterThanFourChars_RedactsCompletely()
    {
        // Arrange
        var apiKey = "ab";

        // Act
        var redacted = SecretsRedactor.RedactApiKey(apiKey);

        // Assert
        redacted.Should().Be("****");
        redacted.Should().NotContain("ab");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RedactApiKey_WithNullOrEmptyInput_ReturnsEmptyString(string? apiKey)
    {
        // Act
        var redacted = SecretsRedactor.RedactApiKey(apiKey);

        // Assert
        redacted.Should().BeEmpty();
    }

    [Fact]
    public void RedactApiKey_WithLongApiKey_RedactsCorrectly()
    {
        // Arrange
        var apiKey = "sk_live_51N3a4b5c6d7e8f9g0h1i2j3k4l5m6n7o8p9q0r1s2t3u4v5w6x7y8z9";

        // Act
        var redacted = SecretsRedactor.RedactApiKey(apiKey);

        // Assert
        redacted.Should().StartWith("sk_l");
        redacted.Should().Contain("****");
        redacted.Should().NotContain("51N3a4b5c6d7e8f9");
    }

    #endregion

    #region Mnemonic Redaction Tests

    [Fact]
    public void RedactMnemonic_WithValidTwelveWordMnemonic_ShowsFirstTwoWordsAndRedacted()
    {
        // Arrange
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

        // Act
        var redacted = SecretsRedactor.RedactMnemonic(mnemonic);

        // Assert
        redacted.Should().Be("abandon abandon [...REDACTED]");
        redacted.Should().NotContain("about");
    }

    [Fact]
    public void RedactMnemonic_WithTwentyFourWordMnemonic_ShowsFirstTwoWordsAndRedacted()
    {
        // Arrange
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon art";

        // Act
        var redacted = SecretsRedactor.RedactMnemonic(mnemonic);

        // Assert
        redacted.Should().Be("abandon abandon [...REDACTED]");
        redacted.Should().NotContain("art");
    }

    [Fact]
    public void RedactMnemonic_WithTwoWordMnemonic_ShowsBothWordsAndRedacted()
    {
        // Arrange
        var mnemonic = "word1 word2";

        // Act
        var redacted = SecretsRedactor.RedactMnemonic(mnemonic);

        // Assert
        redacted.Should().Be("word1 word2 [...REDACTED]");
    }

    [Fact]
    public void RedactMnemonic_WithSingleWord_ShowsWordAndRedacted()
    {
        // Arrange
        var mnemonic = "singleword";

        // Act
        var redacted = SecretsRedactor.RedactMnemonic(mnemonic);

        // Assert
        redacted.Should().Be("singleword [...REDACTED]");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RedactMnemonic_WithNullOrEmptyInput_ReturnsEmptyString(string? mnemonic)
    {
        // Act
        var redacted = SecretsRedactor.RedactMnemonic(mnemonic);

        // Assert
        redacted.Should().BeEmpty();
    }

    [Fact]
    public void RedactMnemonic_WithExtraWhitespace_HandlesCorrectly()
    {
        // Arrange
        var mnemonic = "  word1   word2   word3  word4  ";

        // Act
        var redacted = SecretsRedactor.RedactMnemonic(mnemonic);

        // Assert
        redacted.Should().Be("word1 word2 [...REDACTED]");
    }

    #endregion

    #region Webhook Secret Redaction Tests

    [Fact]
    public void RedactWebhookSecret_WithValidSecret_ReturnsCompletelyRedacted()
    {
        // Arrange
        var secret = "whsec_1234567890abcdefghijklmnop";

        // Act
        var redacted = SecretsRedactor.RedactWebhookSecret(secret);

        // Assert
        redacted.Should().Be("[REDACTED]");
        redacted.Should().NotContain("whsec");
        redacted.Should().NotContain("1234567890");
    }

    [Fact]
    public void RedactWebhookSecret_WithShortSecret_ReturnsCompletelyRedacted()
    {
        // Arrange
        var secret = "abc";

        // Act
        var redacted = SecretsRedactor.RedactWebhookSecret(secret);

        // Assert
        redacted.Should().Be("[REDACTED]");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RedactWebhookSecret_WithNullOrEmptyInput_ReturnsEmptyString(string? secret)
    {
        // Act
        var redacted = SecretsRedactor.RedactWebhookSecret(secret);

        // Assert
        redacted.Should().BeEmpty();
    }

    #endregion

    #region RedactOptions Method Tests

    [Fact]
    public void RedactOptions_WithValidOptions_RedactsAllSensitiveData()
    {
        // Arrange
        var options = CreateOptionsWithSecrets();

        // Act
        var redacted = SecretsRedactor.RedactOptions(options);

        // Assert
        redacted.Should().NotBeNull();

        // Verify API key is redacted
        redacted.ApiKey.Should().StartWith("test");
        redacted.ApiKey.Should().Contain("****");
        redacted.ApiKey.Should().NotContain("12345678");

        // Verify mnemonic is redacted
        redacted.Mnemonic.Should().Be("abandon abandon [...REDACTED]");
        redacted.Mnemonic.Should().NotContain("about");

        // Verify webhook secret is redacted
        redacted.WebhookSecret.Should().Be("[REDACTED]");
        redacted.WebhookSecret.Should().NotContain("whsec_secret123");

        // Verify non-sensitive data is preserved
        redacted.Network.Should().Be(options.Network);
        redacted.WorkingDirectory.Should().Be(options.WorkingDirectory);
        redacted.WebhookUrl.Should().Be(options.WebhookUrl);
        redacted.MaxInvoiceAmountSat.Should().Be(options.MaxInvoiceAmountSat);
        redacted.ConnectionTimeoutSeconds.Should().Be(options.ConnectionTimeoutSeconds);
        redacted.OfflineMode.Should().Be(options.OfflineMode);
    }

    [Fact]
    public void RedactOptions_WithNullSecrets_HandlesGracefully()
    {
        // Arrange
        var options = new BreezSdkOptions
        {
            ApiKey = null,
            Mnemonic = null,
            WebhookSecret = null,
            WorkingDirectory = @"C:\temp\breez",
            Network = BreezNetwork.Testnet
        };

        // Act
        var redacted = SecretsRedactor.RedactOptions(options);

        // Assert
        redacted.Should().NotBeNull();
        redacted.ApiKey.Should().BeEmpty();
        redacted.Mnemonic.Should().BeEmpty();
        redacted.WebhookSecret.Should().BeEmpty();
        redacted.WorkingDirectory.Should().Be(options.WorkingDirectory);
        redacted.Network.Should().Be(options.Network);
    }

    [Fact]
    public void RedactOptions_WithEmptySecrets_HandlesGracefully()
    {
        // Arrange
        var options = new BreezSdkOptions
        {
            ApiKey = "",
            Mnemonic = "   ",
            WebhookSecret = "",
            WorkingDirectory = @"C:\temp\breez",
            Network = BreezNetwork.Mainnet
        };

        // Act
        var redacted = SecretsRedactor.RedactOptions(options);

        // Assert
        redacted.Should().NotBeNull();
        redacted.ApiKey.Should().BeEmpty();
        redacted.Mnemonic.Should().BeEmpty();
        redacted.WebhookSecret.Should().BeEmpty();
    }

    [Fact]
    public void RedactOptions_PreservesCircuitBreakerSettings()
    {
        // Arrange
        var options = CreateOptionsWithSecrets();
        options.CircuitBreaker = new CircuitBreakerOptions
        {
            FailureThreshold = 10,
            SamplingDurationSeconds = 120,
            BreakDurationSeconds = 60
        };

        // Act
        var redacted = SecretsRedactor.RedactOptions(options);

        // Assert
        redacted.CircuitBreaker.Should().NotBeNull();
        redacted.CircuitBreaker.FailureThreshold.Should().Be(10);
        redacted.CircuitBreaker.SamplingDurationSeconds.Should().Be(120);
        redacted.CircuitBreaker.BreakDurationSeconds.Should().Be(60);
    }

    [Fact]
    public void RedactOptions_PreservesOfflineModeSettings()
    {
        // Arrange
        var options = CreateOptionsWithSecrets();
        options.OfflineMode = true;
        options.OfflineSimulateDelayMs = 500;
        options.OfflineSimulateFailureRate = 0.1;
        options.OfflineMockBalanceSat = 200_000;

        // Act
        var redacted = SecretsRedactor.RedactOptions(options);

        // Assert
        redacted.OfflineMode.Should().BeTrue();
        redacted.OfflineSimulateDelayMs.Should().Be(500);
        redacted.OfflineSimulateFailureRate.Should().Be(0.1);
        redacted.OfflineMockBalanceSat.Should().Be(200_000);
    }

    [Fact]
    public void RedactOptions_CreatesNewInstance_DoesNotModifyOriginal()
    {
        // Arrange
        var original = CreateOptionsWithSecrets();
        var originalApiKey = original.ApiKey;
        var originalMnemonic = original.Mnemonic;
        var originalWebhookSecret = original.WebhookSecret;

        // Act
        var redacted = SecretsRedactor.RedactOptions(original);

        // Assert
        original.ApiKey.Should().Be(originalApiKey, "original should not be modified");
        original.Mnemonic.Should().Be(originalMnemonic, "original should not be modified");
        original.WebhookSecret.Should().Be(originalWebhookSecret, "original should not be modified");

        redacted.Should().NotBeSameAs(original, "should create a new instance");
    }

    [Fact]
    public void RedactOptions_WithNullInput_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => SecretsRedactor.RedactOptions(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a BreezSdkOptions instance with all secrets populated for testing.
    /// </summary>
    private static BreezSdkOptions CreateOptionsWithSecrets() => new()
    {
        ApiKey = "test-api-key-12345678",
        Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about",
        WebhookSecret = "whsec_secret123",
        WorkingDirectory = @"C:\temp\breez-test",
        Network = BreezNetwork.Testnet,
        WebhookUrl = "https://example.com/webhook",
        MaxInvoiceAmountSat = 1_000_000,
        MaxInvoiceDescriptionLength = 200,
        ConnectionTimeoutSeconds = 30,
        OfflineMode = false,
        CircuitBreaker = new CircuitBreakerOptions
        {
            FailureThreshold = 5,
            SamplingDurationSeconds = 60,
            BreakDurationSeconds = 30
        }
    };

    #endregion
}
