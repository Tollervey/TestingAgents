using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Notification;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services;

public class WebhookNotificationHandlerTests
{
    private readonly IOptions<NotificationOptions> _options;
    private readonly Mock<ILogger<WebhookNotificationHandler>> _loggerMock;

    public WebhookNotificationHandlerTests()
    {
        _options = Options.Create(new NotificationOptions
        {
            Webhook = new WebhookNotificationOptions
            {
                Secret = "test-secret-key",
                TimeoutSeconds = 30
            }
        });
        _loggerMock = new Mock<ILogger<WebhookNotificationHandler>>();
    }

    [Fact]
    public void HandlerType_ReturnsWebhook()
    {
        var handler = CreateHandler(new HttpResponseMessage(HttpStatusCode.OK));
        handler.HandlerType.Should().Be(NotificationType.Webhook);
    }

    [Fact]
    public async Task SendAsync_WhenSuccess_ReturnsSucceeded()
    {
        // Arrange
        var handler = CreateHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var notification = CreateTestNotification();

        // Act
        var result = await handler.SendAsync(notification);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_WhenServerError_ReturnsFailedWithStatusCode()
    {
        // Arrange
        var handler = CreateHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var notification = CreateTestNotification();

        // Act
        var result = await handler.SendAsync(notification);

        // Assert
        result.Success.Should().BeFalse();
        result.HttpStatusCode.Should().Be(500);
        result.ErrorMessage.Should().Contain("500");
    }

    [Fact]
    public async Task SendAsync_WhenClientError_ReturnsFailedWithStatusCode()
    {
        // Arrange
        var handler = CreateHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var notification = CreateTestNotification();

        // Act
        var result = await handler.SendAsync(notification);

        // Assert
        result.Success.Should().BeFalse();
        result.HttpStatusCode.Should().Be(400);
    }

    [Fact]
    public async Task SendAsync_WhenNetworkException_ReturnsFailedWithErrorMessage()
    {
        // Arrange
        var mockHandler = new ExceptionThrowingHandler(new HttpRequestException("Connection refused"));
        var httpClient = new HttpClient(mockHandler);
        var handler = new WebhookNotificationHandler(httpClient, _options, _loggerMock.Object);
        var notification = CreateTestNotification();

        // Act
        var result = await handler.SendAsync(notification);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Connection refused");
    }

    [Fact]
    public async Task SendAsync_WhenDestinationEmpty_ReturnsFailedWithConfigError()
    {
        // Arrange
        var handler = CreateHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var notification = CreateTestNotification(destination: string.Empty);

        // Act
        var result = await handler.SendAsync(notification);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Webhook URL is not configured");
    }

    [Fact]
    public async Task SendAsync_IncludesSignatureHeaders()
    {
        // Arrange
        var mockHandler = new CapturingHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(mockHandler);
        var handler = new WebhookNotificationHandler(httpClient, _options, _loggerMock.Object);
        var notification = CreateTestNotification();

        // Act
        await handler.SendAsync(notification);

        // Assert
        var request = mockHandler.LastRequest!;
        request.Headers.Contains("X-Lightning-Signature").Should().BeTrue();
        request.Headers.Contains("X-Lightning-Timestamp").Should().BeTrue();
        request.Headers.Contains("X-Lightning-Webhook-Id").Should().BeTrue();
        request.Headers.Contains("X-Lightning-Event").Should().BeTrue();
        request.Headers.Contains("X-Lightning-Attempt").Should().BeTrue();
    }

    [Fact]
    public void ComputeHmacSignature_ProducesCorrectSignature()
    {
        // Per webhook-api.yaml: HMAC-SHA256(secret, timestamp + "." + body)
        var timestamp = "1706270400";
        var body = "{\"event\":\"payment.confirmed\"}";
        var secret = "test-secret-key";

        var signature = WebhookNotificationHandler.ComputeHmacSignature(timestamp, body, secret);

        signature.Should().NotBeNullOrEmpty();
        signature.Should().MatchRegex("^[0-9a-f]{64}$"); // SHA256 = 64 hex chars

        // Verify deterministic
        var signature2 = WebhookNotificationHandler.ComputeHmacSignature(timestamp, body, secret);
        signature.Should().Be(signature2);
    }

    [Fact]
    public void ComputeHmacSignature_DifferentSecrets_ProduceDifferentSignatures()
    {
        var timestamp = "1706270400";
        var body = "{\"test\":true}";

        var sig1 = WebhookNotificationHandler.ComputeHmacSignature(timestamp, body, "secret1");
        var sig2 = WebhookNotificationHandler.ComputeHmacSignature(timestamp, body, "secret2");

        sig1.Should().NotBe(sig2);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        var act = () => new WebhookNotificationHandler(null!, _options, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new WebhookNotificationHandler(new HttpClient(), null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new WebhookNotificationHandler(new HttpClient(), _options, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region Helpers

    private WebhookNotificationHandler CreateHandler(HttpResponseMessage response)
    {
        var mockHandler = new CapturingHandler(response);
        var httpClient = new HttpClient(mockHandler);
        return new WebhookNotificationHandler(httpClient, _options, _loggerMock.Object);
    }

    private static PaymentNotification CreateTestNotification(
        string destination = "https://webhook.test.com/hook") => new()
    {
        NotificationId = Guid.NewGuid(),
        PaymentHash = "test_payment_hash",
        Type = NotificationType.Webhook,
        Event = NotificationEvent.PaymentConfirmed,
        Destination = destination,
        Status = NotificationStatus.Pending,
        AttemptCount = 1,
        MaxAttempts = 5,
        CreatedAt = DateTimeOffset.UtcNow,
        Payload = "{\"event\":\"payment.confirmed\",\"data\":{\"paymentHash\":\"test_payment_hash\"}}"
    };

    private class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public HttpRequestMessage? LastRequest { get; private set; }

        public CapturingHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            return Task.FromResult(_response);
        }
    }

    private class ExceptionThrowingHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ExceptionThrowingHandler(Exception exception) => _exception = exception;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            throw _exception;
        }
    }

    #endregion
}
