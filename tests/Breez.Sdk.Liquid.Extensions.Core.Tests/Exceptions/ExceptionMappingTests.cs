using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.Core.Exceptions;
using FluentAssertions;

namespace Breez.Sdk.Liquid.Extensions.Core.Tests.Exceptions;

/// <summary>
/// Tests for exception categorization and mapping functionality.
/// Verifies that system and SDK exceptions are correctly mapped to BreezSdkException hierarchy.
/// </summary>
/// <remarks>
/// These tests follow TDD principles and will initially fail until ExceptionMapper is implemented.
/// Tests verify:
/// - SDK exceptions mapped to appropriate BreezSdkException types
/// - Error codes correctly assigned based on exception type
/// - IsRetryable flag set correctly
/// - Exception chaining preserved
/// - Messages preserved or enhanced
/// </remarks>
public class ExceptionMappingTests
{
    #region TimeoutException Mapping Tests

    [Fact]
    public void Map_TimeoutException_ReturnsConnectionException()
    {
        // Arrange
        var timeoutException = new TimeoutException("Connection timed out after 30 seconds");

        // Act
        var result = ExceptionMapper.Map(timeoutException);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ConnectionException>();
    }

    [Fact]
    public void Map_TimeoutException_SetsConnectionTimeoutErrorCode()
    {
        // Arrange
        var timeoutException = new TimeoutException("Connection timed out");

        // Act
        var result = ExceptionMapper.Map(timeoutException);

        // Assert
        result.ErrorCode.Should().Be(BreezErrorCode.ConnectionTimeout);
    }

    [Fact]
    public void Map_TimeoutException_MarksAsRetryable()
    {
        // Arrange
        var timeoutException = new TimeoutException("Timeout occurred");

        // Act
        var result = ExceptionMapper.Map(timeoutException);

        // Assert
        result.IsRetryable.Should().BeTrue();
    }

    [Fact]
    public void Map_TimeoutException_PreservesInnerException()
    {
        // Arrange
        var timeoutException = new TimeoutException("Timeout occurred");

        // Act
        var result = ExceptionMapper.Map(timeoutException);

        // Assert
        result.InnerException.Should().BeSameAs(timeoutException);
    }

    [Fact]
    public void Map_TimeoutException_EnhancesMessageWithContext()
    {
        // Arrange
        var timeoutException = new TimeoutException("Operation timed out");

        // Act
        var result = ExceptionMapper.Map(timeoutException);

        // Assert
        result.Message.Should().Contain("timeout");
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region HttpRequestException Mapping Tests

    [Fact]
    public void Map_HttpRequestException_ReturnsTransientException()
    {
        // Arrange
        var httpException = new HttpRequestException("Network error occurred");

        // Act
        var result = ExceptionMapper.Map(httpException);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<TransientException>();
    }

    [Fact]
    public void Map_HttpRequestException_SetsNetworkErrorCode()
    {
        // Arrange
        var httpException = new HttpRequestException("Failed to connect");

        // Act
        var result = ExceptionMapper.Map(httpException);

        // Assert
        result.ErrorCode.Should().Be(BreezErrorCode.NetworkError);
    }

    [Fact]
    public void Map_HttpRequestException_MarksAsRetryable()
    {
        // Arrange
        var httpException = new HttpRequestException("Network unreachable");

        // Act
        var result = ExceptionMapper.Map(httpException);

        // Assert
        result.IsRetryable.Should().BeTrue();
    }

    [Fact]
    public void Map_HttpRequestException_PreservesInnerException()
    {
        // Arrange
        var httpException = new HttpRequestException("Request failed");

        // Act
        var result = ExceptionMapper.Map(httpException);

        // Assert
        result.InnerException.Should().BeSameAs(httpException);
    }

    [Fact]
    public void Map_HttpRequestException_PreservesOriginalMessage()
    {
        // Arrange
        var originalMessage = "DNS resolution failed for api.breez.technology";
        var httpException = new HttpRequestException(originalMessage);

        // Act
        var result = ExceptionMapper.Map(httpException);

        // Assert
        result.Message.Should().Contain(originalMessage);
    }

    #endregion

    #region InvalidOperationException Mapping Tests

    [Fact]
    public void Map_InvalidOperationException_WithSdkNotConnectedMessage_ReturnsConnectionException()
    {
        // Arrange
        var invalidOpException = new InvalidOperationException("SDK is not connected");

        // Act
        var result = ExceptionMapper.Map(invalidOpException);

        // Assert
        result.Should().BeOfType<ConnectionException>();
        result.ErrorCode.Should().Be(BreezErrorCode.SdkNotConnected);
    }

    [Fact]
    public void Map_InvalidOperationException_WithInsufficientFundsMessage_ReturnsPaymentException()
    {
        // Arrange
        var invalidOpException = new InvalidOperationException("Insufficient funds to complete payment");

        // Act
        var result = ExceptionMapper.Map(invalidOpException);

        // Assert
        result.Should().BeOfType<PaymentException>();
        result.ErrorCode.Should().Be(BreezErrorCode.InsufficientFunds);
    }

    [Fact]
    public void Map_InvalidOperationException_WithInvalidInvoiceMessage_ReturnsPaymentException()
    {
        // Arrange
        var invalidOpException = new InvalidOperationException("Invalid invoice format");

        // Act
        var result = ExceptionMapper.Map(invalidOpException);

        // Assert
        result.Should().BeOfType<PaymentException>();
        result.ErrorCode.Should().Be(BreezErrorCode.InvalidInvoice);
    }

    [Fact]
    public void Map_InvalidOperationException_WithGenericMessage_ReturnsBreezSdkException()
    {
        // Arrange
        var invalidOpException = new InvalidOperationException("Generic operation failed");

        // Act
        var result = ExceptionMapper.Map(invalidOpException);

        // Assert
        result.Should().BeOfType<BreezSdkException>();
        result.IsRetryable.Should().BeFalse();
    }

    [Fact]
    public void Map_InvalidOperationException_PreservesInnerException()
    {
        // Arrange
        var invalidOpException = new InvalidOperationException("Operation failed");

        // Act
        var result = ExceptionMapper.Map(invalidOpException);

        // Assert
        result.InnerException.Should().BeSameAs(invalidOpException);
    }

    #endregion

    #region UnauthorizedAccessException Mapping Tests

    [Fact]
    public void Map_UnauthorizedAccessException_ReturnsConfigurationException()
    {
        // Arrange
        var unauthorizedException = new UnauthorizedAccessException("API key is invalid");

        // Act
        var result = ExceptionMapper.Map(unauthorizedException);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void Map_UnauthorizedAccessException_SetsConfigurationInvalidErrorCode()
    {
        // Arrange
        var unauthorizedException = new UnauthorizedAccessException("Access denied");

        // Act
        var result = ExceptionMapper.Map(unauthorizedException);

        // Assert
        result.ErrorCode.Should().Be(BreezErrorCode.ConfigurationInvalid);
    }

    [Fact]
    public void Map_UnauthorizedAccessException_MarksAsNotRetryable()
    {
        // Arrange
        var unauthorizedException = new UnauthorizedAccessException("Invalid credentials");

        // Act
        var result = ExceptionMapper.Map(unauthorizedException);

        // Assert
        result.IsRetryable.Should().BeFalse();
    }

    [Fact]
    public void Map_UnauthorizedAccessException_PreservesInnerException()
    {
        // Arrange
        var unauthorizedException = new UnauthorizedAccessException("Forbidden");

        // Act
        var result = ExceptionMapper.Map(unauthorizedException);

        // Assert
        result.InnerException.Should().BeSameAs(unauthorizedException);
    }

    #endregion

    #region ArgumentException Mapping Tests

    [Fact]
    public void Map_ArgumentNullException_WithMnemonicParameter_ReturnsConfigurationException()
    {
        // Arrange
        var argException = new ArgumentNullException("mnemonic", "Mnemonic cannot be null");

        // Act
        var result = ExceptionMapper.Map(argException);

        // Assert
        result.Should().BeOfType<ConfigurationException>();
        result.ErrorCode.Should().Be(BreezErrorCode.MnemonicMissing);
    }

    [Fact]
    public void Map_ArgumentNullException_WithApiKeyParameter_ReturnsConfigurationException()
    {
        // Arrange
        var argException = new ArgumentNullException("apiKey", "API key is required");

        // Act
        var result = ExceptionMapper.Map(argException);

        // Assert
        result.Should().BeOfType<ConfigurationException>();
        result.ErrorCode.Should().Be(BreezErrorCode.ApiKeyMissing);
    }

    [Fact]
    public void Map_ArgumentOutOfRangeException_WithAmountBelowMinimum_ReturnsPaymentException()
    {
        // Arrange
        var argException = new ArgumentOutOfRangeException("amount", "Amount is below minimum limit");

        // Act
        var result = ExceptionMapper.Map(argException);

        // Assert
        result.Should().BeOfType<PaymentException>();
        result.ErrorCode.Should().Be(BreezErrorCode.AmountBelowMinimum);
    }

    [Fact]
    public void Map_ArgumentOutOfRangeException_WithAmountAboveMaximum_ReturnsPaymentException()
    {
        // Arrange
        var argException = new ArgumentOutOfRangeException("amount", "Amount exceeds maximum limit");

        // Act
        var result = ExceptionMapper.Map(argException);

        // Assert
        result.Should().BeOfType<PaymentException>();
        result.ErrorCode.Should().Be(BreezErrorCode.AmountAboveMaximum);
    }

    #endregion

    #region BreezSDK-Specific Exception Mapping Tests

    [Fact]
    public void Map_Exception_WithRateLimitMessage_ReturnsTransientException()
    {
        // Arrange
        var exception = new Exception("Rate limit exceeded, retry after 60 seconds");

        // Act
        var result = ExceptionMapper.Map(exception);

        // Assert
        result.Should().BeOfType<TransientException>();
        result.ErrorCode.Should().Be(BreezErrorCode.RateLimited);
        result.IsRetryable.Should().BeTrue();
    }

    [Fact]
    public void Map_Exception_WithServiceUnavailableMessage_ReturnsTransientException()
    {
        // Arrange
        var exception = new Exception("Service temporarily unavailable");

        // Act
        var result = ExceptionMapper.Map(exception);

        // Assert
        result.Should().BeOfType<TransientException>();
        result.ErrorCode.Should().Be(BreezErrorCode.ServiceUnavailable);
        result.IsRetryable.Should().BeTrue();
    }

    [Fact]
    public void Map_Exception_WithInvoiceExpiredMessage_ReturnsPaymentException()
    {
        // Arrange
        var exception = new Exception("Invoice has expired");

        // Act
        var result = ExceptionMapper.Map(exception);

        // Assert
        result.Should().BeOfType<PaymentException>();
        result.ErrorCode.Should().Be(BreezErrorCode.InvoiceExpired);
        result.IsRetryable.Should().BeFalse();
    }

    [Fact]
    public void Map_Exception_WithDisconnectedMessage_ReturnsConnectionException()
    {
        // Arrange
        var exception = new Exception("SDK was disconnected unexpectedly");

        // Act
        var result = ExceptionMapper.Map(exception);

        // Assert
        result.Should().BeOfType<ConnectionException>();
        result.ErrorCode.Should().Be(BreezErrorCode.SdkDisconnected);
        result.IsRetryable.Should().BeTrue();
    }

    #endregion

    #region Exception Chaining Tests

    [Fact]
    public void Map_NestedExceptions_PreservesFullExceptionChain()
    {
        // Arrange
        var innermost = new InvalidOperationException("Root cause");
        var middle = new HttpRequestException("Network error", innermost);
        var outer = new TimeoutException("Operation timed out", middle);

        // Act
        var result = ExceptionMapper.Map(outer);

        // Assert
        result.InnerException.Should().BeSameAs(outer);
        result.InnerException!.InnerException.Should().BeSameAs(middle);
        result.InnerException!.InnerException!.InnerException.Should().BeSameAs(innermost);
    }

    [Fact]
    public void Map_ExceptionWithoutInnerException_HandlesGracefully()
    {
        // Arrange
        var exception = new TimeoutException("Timeout without inner exception");

        // Act
        var result = ExceptionMapper.Map(exception);

        // Assert
        result.InnerException.Should().BeSameAs(exception);
        result.InnerException!.InnerException.Should().BeNull();
    }

    #endregion

    #region Message Preservation and Enhancement Tests

    [Fact]
    public void Map_Exception_PreservesOriginalMessageContent()
    {
        // Arrange
        var originalMessage = "Specific error with important details: error_code_123";
        var exception = new InvalidOperationException(originalMessage);

        // Act
        var result = ExceptionMapper.Map(exception);

        // Assert
        result.Message.Should().Contain("error_code_123");
    }

    [Fact]
    public void Map_Exception_EnhancesMessageWithContextWhenAppropriate()
    {
        // Arrange
        var exception = new TimeoutException("Timeout");

        // Act
        var result = ExceptionMapper.Map(exception);

        // Assert
        result.Message.Should().NotBeNullOrWhiteSpace();
        // Should contain contextual information, not just "Timeout"
        result.Message.Length.Should().BeGreaterThan(exception.Message.Length);
    }

    #endregion

    #region Edge Cases and Null Handling

    [Fact]
    public void Map_NullException_ThrowsArgumentNullException()
    {
        // Arrange
        Exception? nullException = null;

        // Act
        Action act = () => ExceptionMapper.Map(nullException!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("exception");
    }

    [Fact]
    public void Map_ExceptionWithNullMessage_HandlesGracefully()
    {
        // Arrange - Some exception types allow null messages
        var exception = new Exception((string?)null);

        // Act
        var result = ExceptionMapper.Map(exception);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().NotBeNull(); // Should provide a default message
    }

    [Fact]
    public void Map_ExceptionWithEmptyMessage_ProvidesDefaultMessage()
    {
        // Arrange
        var exception = new Exception(string.Empty);

        // Act
        var result = ExceptionMapper.Map(exception);

        // Assert
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Already-Mapped Exception Tests

    [Fact]
    public void Map_BreezSdkException_ReturnsAsIs()
    {
        // Arrange
        var breezException = new ConnectionException(
            BreezErrorCode.ConnectionTimeout,
            "Already a BreezSdkException");

        // Act
        var result = ExceptionMapper.Map(breezException);

        // Assert
        result.Should().BeSameAs(breezException);
    }

    [Fact]
    public void Map_ConfigurationException_ReturnsAsIs()
    {
        // Arrange
        var configException = new ConfigurationException(
            BreezErrorCode.ApiKeyMissing,
            "API key is missing",
            "apiKey");

        // Act
        var result = ExceptionMapper.Map(configException);

        // Assert
        result.Should().BeSameAs(configException);
    }

    [Fact]
    public void Map_PaymentException_ReturnsAsIs()
    {
        // Arrange
        var paymentException = new PaymentException(
            BreezErrorCode.InsufficientFunds,
            "Not enough balance",
            "payment_hash_123");

        // Act
        var result = ExceptionMapper.Map(paymentException);

        // Assert
        result.Should().BeSameAs(paymentException);
    }

    [Fact]
    public void Map_TransientException_ReturnsAsIs()
    {
        // Arrange
        var transientException = new TransientException(
            BreezErrorCode.RateLimited,
            "Too many requests",
            TimeSpan.FromSeconds(60));

        // Act
        var result = ExceptionMapper.Map(transientException);

        // Assert
        result.Should().BeSameAs(transientException);
    }

    #endregion

    #region Case-Insensitive Message Matching Tests

    [Fact]
    public void Map_Exception_WithLowercaseKeywords_MatchesCorrectly()
    {
        // Arrange
        var exception = new Exception("insufficient funds available");

        // Act
        var result = ExceptionMapper.Map(exception);

        // Assert
        result.Should().BeOfType<PaymentException>();
        result.ErrorCode.Should().Be(BreezErrorCode.InsufficientFunds);
    }

    [Fact]
    public void Map_Exception_WithMixedCaseKeywords_MatchesCorrectly()
    {
        // Arrange
        var exception = new Exception("InVoIcE HaS ExPiReD");

        // Act
        var result = ExceptionMapper.Map(exception);

        // Assert
        result.Should().BeOfType<PaymentException>();
        result.ErrorCode.Should().Be(BreezErrorCode.InvoiceExpired);
    }

    #endregion

    #region Generic Exception Fallback Tests

    [Fact]
    public void Map_UnrecognizedException_ReturnsGenericBreezSdkException()
    {
        // Arrange
        var exception = new NotImplementedException("Feature not yet implemented");

        // Act
        var result = ExceptionMapper.Map(exception);

        // Assert
        result.Should().BeOfType<BreezSdkException>();
        result.IsRetryable.Should().BeFalse();
    }

    [Fact]
    public void Map_CustomException_WithNoSpecialHandling_ReturnsGenericBreezSdkException()
    {
        // Arrange
        var exception = new CustomTestException("Custom error");

        // Act
        var result = ExceptionMapper.Map(exception);

        // Assert
        result.Should().BeOfType<BreezSdkException>();
        result.InnerException.Should().BeSameAs(exception);
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Custom exception type for testing generic exception mapping.
    /// </summary>
    private class CustomTestException : Exception
    {
        public CustomTestException(string message) : base(message)
        {
        }
    }

    #endregion
}
