using Breez.Sdk.Liquid.Extensions.Core.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Configuration;

public class BreezSdkOptionsValidatorTests
{
    private readonly BreezSdkOptionsValidator _validator = new();

    [Fact]
    public void Validate_WithValidOptions_ReturnsSuccess()
    {
        // Arrange
        var options = CreateValidOptions();

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.FailureMessage.Should().BeNullOrEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptyApiKey_ReturnsFailed(string? apiKey)
    {
        // Arrange
        var options = CreateValidOptions();
        options.ApiKey = apiKey;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("ApiKey");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptyMnemonic_ReturnsFailed(string? mnemonic)
    {
        // Arrange
        var options = CreateValidOptions();
        options.Mnemonic = mnemonic;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Mnemonic");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithNullOrEmptyWorkingDirectory_ReturnsFailed(string? workingDirectory)
    {
        // Arrange
        var options = CreateValidOptions();
        options.WorkingDirectory = workingDirectory;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("WorkingDirectory");
    }

    [Fact]
    public void Validate_WithInvalidNetworkEnum_ReturnsFailed()
    {
        // Arrange
        var options = CreateValidOptions();
        options.Network = (BreezNetwork)999; // Invalid enum value

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Network");
    }

    [Fact]
    public void Validate_WithOfflineModeEnabled_AllowsMissingApiKey()
    {
        // Arrange
        var options = CreateValidOptions();
        options.OfflineMode = true;
        options.ApiKey = null;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithOfflineModeEnabled_AllowsMissingMnemonic()
    {
        // Arrange
        var options = CreateValidOptions();
        options.OfflineMode = true;
        options.Mnemonic = null;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithOfflineModeEnabled_StillRequiresWorkingDirectory()
    {
        // Arrange
        var options = CreateValidOptions();
        options.OfflineMode = true;
        options.WorkingDirectory = null;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("WorkingDirectory");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://invalid-scheme.com")]
    [InlineData("http://")]
    [InlineData("://missing-scheme.com")]
    public void Validate_WithMalformedWebhookUrl_ReturnsFailed(string malformedUrl)
    {
        // Arrange
        var options = CreateValidOptions();
        options.WebhookUrl = malformedUrl;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("WebhookUrl");
    }

    [Theory]
    [InlineData("https://valid-webhook.com/hook")]
    [InlineData("http://localhost:3000/webhook")]
    [InlineData(null)] // Webhook is optional
    public void Validate_WithValidOrNullWebhookUrl_ReturnsSuccess(string? webhookUrl)
    {
        // Arrange
        var options = CreateValidOptions();
        options.WebhookUrl = webhookUrl;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithZeroMaxInvoiceAmount_ReturnsFailed()
    {
        // Arrange
        var options = CreateValidOptions();
        options.MaxInvoiceAmountSat = 0;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("MaxInvoiceAmountSat");
    }

    [Theory]
    [InlineData(1000)]
    [InlineData(100000)]
    [InlineData(21000000 * 100000000UL)] // Max possible satoshis
    public void Validate_WithValidMaxInvoiceAmount_ReturnsSuccess(ulong maxAmount)
    {
        // Arrange
        var options = CreateValidOptions();
        options.MaxInvoiceAmountSat = maxAmount;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-60)]
    public void Validate_WithZeroOrNegativeConnectionTimeout_ReturnsFailed(int timeout)
    {
        // Arrange
        var options = CreateValidOptions();
        options.ConnectionTimeoutSeconds = timeout;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("ConnectionTimeoutSeconds");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(300)]
    [InlineData(3600)]
    public void Validate_WithValidConnectionTimeout_ReturnsSuccess(int timeout)
    {
        // Arrange
        var options = CreateValidOptions();
        options.ConnectionTimeoutSeconds = timeout;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithZeroOrNegativeCircuitBreakerFailureThreshold_ReturnsFailed(int threshold)
    {
        // Arrange
        var options = CreateValidOptions();
        options.CircuitBreaker.FailureThreshold = threshold;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("CircuitBreaker.FailureThreshold");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    public void Validate_WithValidCircuitBreakerFailureThreshold_ReturnsSuccess(int threshold)
    {
        // Arrange
        var options = CreateValidOptions();
        options.CircuitBreaker.FailureThreshold = threshold;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithZeroOrNegativeCircuitBreakerSamplingDuration_ReturnsFailed(int duration)
    {
        // Arrange
        var options = CreateValidOptions();
        options.CircuitBreaker.SamplingDurationSeconds = duration;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("CircuitBreaker.SamplingDurationSeconds");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(300)]
    public void Validate_WithValidCircuitBreakerSamplingDuration_ReturnsSuccess(int duration)
    {
        // Arrange
        var options = CreateValidOptions();
        options.CircuitBreaker.SamplingDurationSeconds = duration;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithZeroOrNegativeCircuitBreakerBreakDuration_ReturnsFailed(int duration)
    {
        // Arrange
        var options = CreateValidOptions();
        options.CircuitBreaker.BreakDurationSeconds = duration;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("CircuitBreaker.BreakDurationSeconds");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(300)]
    public void Validate_WithValidCircuitBreakerBreakDuration_ReturnsSuccess(int duration)
    {
        // Arrange
        var options = CreateValidOptions();
        options.CircuitBreaker.BreakDurationSeconds = duration;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullCircuitBreakerOptions_ReturnsFailed()
    {
        // Arrange
        var options = CreateValidOptions();
        options.CircuitBreaker = null!;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("CircuitBreaker");
    }

    [Fact]
    public void Validate_WithMultipleValidationErrors_ReturnsAllFailures()
    {
        // Arrange
        var options = new BreezSdkOptions
        {
            ApiKey = null,
            Mnemonic = "",
            WorkingDirectory = "   ",
            Network = (BreezNetwork)999,
            WebhookUrl = "not-a-url",
            MaxInvoiceAmountSat = 0,
            ConnectionTimeoutSeconds = -1,
            CircuitBreaker = new CircuitBreakerOptions
            {
                FailureThreshold = 0,
                SamplingDurationSeconds = -1,
                BreakDurationSeconds = 0
            }
        };

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCountGreaterThan(5);
        result.Failures.Should().Contain(f => f.Contains("ApiKey"));
        result.Failures.Should().Contain(f => f.Contains("Mnemonic"));
        result.Failures.Should().Contain(f => f.Contains("WorkingDirectory"));
        result.Failures.Should().Contain(f => f.Contains("Network"));
        result.Failures.Should().Contain(f => f.Contains("WebhookUrl"));
    }

    [Theory]
    [InlineData(BreezNetwork.Mainnet)]
    [InlineData(BreezNetwork.Testnet)]
    public void Validate_WithValidNetworkEnum_ReturnsSuccess(BreezNetwork network)
    {
        // Arrange
        var options = CreateValidOptions();
        options.Network = network;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Creates a valid BreezSdkOptions instance for testing.
    /// All tests should use this as a baseline and modify specific properties.
    /// </summary>
    private static BreezSdkOptions CreateValidOptions() => new()
    {
        ApiKey = "test-api-key-12345",
        Mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about",
        WorkingDirectory = @"C:\temp\breez-test",
        Network = BreezNetwork.Testnet,
        WebhookUrl = null, // Optional
        MaxInvoiceAmountSat = 1000000, // 0.01 BTC
        ConnectionTimeoutSeconds = 30,
        OfflineMode = false,
        CircuitBreaker = new CircuitBreakerOptions
        {
            FailureThreshold = 5,
            SamplingDurationSeconds = 60,
            BreakDurationSeconds = 30
        }
    };
}
