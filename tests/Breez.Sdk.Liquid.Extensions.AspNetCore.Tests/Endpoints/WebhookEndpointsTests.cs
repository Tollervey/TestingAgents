using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Breez.Sdk.Liquid.Extensions.AspNetCore.Endpoints;
using Breez.Sdk.Liquid.Extensions.Core.Abstractions;
using Breez.Sdk.Liquid.Extensions.Core.Domain;
using Breez.Sdk.Liquid.Extensions.Core.Domain.Events;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Breez.Sdk.Liquid.Extensions.AspNetCore.Tests.Endpoints;

/// <summary>
/// Unit tests for WebhookEndpoints.
/// Tests follow TDD RED-GREEN-REFACTOR pattern.
/// These tests are written FIRST and will FAIL until the implementation is created.
/// </summary>
public class WebhookEndpointsTests : IDisposable
{
    private readonly Mock<IPaymentEventChannel> _mockEventChannel;
    private readonly TestServer _server;
    private readonly HttpClient _client;

    public WebhookEndpointsTests()
    {
        _mockEventChannel = new Mock<IPaymentEventChannel>();
        _mockEventChannel
            .Setup(x => x.PublishAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(_mockEventChannel.Object);
                services.AddLogging();
                services.AddRouting();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapBreezSdkEndpoints();
                });
            });

        _server = new TestServer(builder);
        _client = _server.CreateClient();
    }

    [Fact]
    public async Task PostWebhook_WithValidPaymentReceivedEvent_Returns200()
    {
        // Arrange
        var payload = new WebhookPayload
        {
            EventType = "payment_received",
            PaymentHash = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2",
            AmountSat = 5000,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/breez/webhook", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WebhookResponse>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task PostWebhook_WithValidPaymentConfirmedEvent_PublishesToChannel()
    {
        // Arrange
        var payload = new WebhookPayload
        {
            EventType = "payment_confirmed",
            PaymentHash = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2",
            AmountSat = 5000,
            Preimage = "b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3",
            FeeSat = 10,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        await _client.PostAsJsonAsync("/api/breez/webhook", payload);

        // Assert
        _mockEventChannel.Verify(
            x => x.PublishAsync(
                It.Is<PaymentConfirmed>(e =>
                    e.PaymentHash == payload.PaymentHash &&
                    e.AmountSat == payload.AmountSat &&
                    e.Preimage == payload.Preimage),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostWebhook_WithValidPaymentFailedEvent_PublishesFailureEvent()
    {
        // Arrange
        var payload = new WebhookPayload
        {
            EventType = "payment_failed",
            PaymentHash = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2",
            ErrorCode = 3002,
            ErrorMessage = "Payment route not found",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        await _client.PostAsJsonAsync("/api/breez/webhook", payload);

        // Assert
        _mockEventChannel.Verify(
            x => x.PublishAsync(
                It.Is<PaymentFailed>(e =>
                    e.PaymentHash == payload.PaymentHash &&
                    e.ErrorCode == BreezErrorCode.PaymentFailed &&
                    e.Reason == payload.ErrorMessage),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostWebhook_WithValidInvoiceCreatedEvent_PublishesInvoiceEvent()
    {
        // Arrange
        var payload = new WebhookPayload
        {
            EventType = "invoice_created",
            PaymentHash = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2",
            AmountSat = 10000,
            Invoice = "lnbc10n1pj9abc...",
            Description = "Test payment",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        await _client.PostAsJsonAsync("/api/breez/webhook", payload);

        // Assert
        _mockEventChannel.Verify(
            x => x.PublishAsync(
                It.Is<InvoiceCreated>(e =>
                    e.PaymentHash == payload.PaymentHash &&
                    e.AmountSat == payload.AmountSat),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostWebhook_WithValidInvoiceExpiredEvent_PublishesExpiredEvent()
    {
        // Arrange
        var payload = new WebhookPayload
        {
            EventType = "invoice_expired",
            PaymentHash = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2",
            ExpiredAt = DateTimeOffset.UtcNow,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        await _client.PostAsJsonAsync("/api/breez/webhook", payload);

        // Assert
        _mockEventChannel.Verify(
            x => x.PublishAsync(
                It.Is<InvoiceExpired>(e => e.PaymentHash == payload.PaymentHash),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostWebhook_WithMissingEventType_Returns400()
    {
        // Arrange
        var payload = new { PaymentHash = "abc123", Timestamp = DateTimeOffset.UtcNow };

        // Act
        var response = await _client.PostAsJsonAsync("/api/breez/webhook", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostWebhook_WithMissingPaymentHash_Returns400()
    {
        // Arrange
        var payload = new { EventType = "payment_received", Timestamp = DateTimeOffset.UtcNow };

        // Act
        var response = await _client.PostAsJsonAsync("/api/breez/webhook", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostWebhook_WithUnknownEventType_Returns400()
    {
        // Arrange
        var payload = new WebhookPayload
        {
            EventType = "unknown_event_type",
            PaymentHash = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/breez/webhook", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostWebhook_WhenChannelPublishFails_Returns500()
    {
        // Arrange
        _mockEventChannel
            .Setup(x => x.PublishAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Channel is full"));

        var payload = new WebhookPayload
        {
            EventType = "payment_received",
            PaymentHash = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2",
            AmountSat = 5000,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/breez/webhook", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task PostWebhook_IncludesCorrelationIdInEvent()
    {
        // Arrange
        var correlationId = "req-12345";
        var payload = new WebhookPayload
        {
            EventType = "payment_received",
            PaymentHash = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2",
            AmountSat = 5000,
            Timestamp = DateTimeOffset.UtcNow,
            CorrelationId = correlationId
        };

        // Act
        await _client.PostAsJsonAsync("/api/breez/webhook", payload);

        // Assert
        _mockEventChannel.Verify(
            x => x.PublishAsync(
                It.Is<PaymentReceived>(e => e.CorrelationId == correlationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PostWebhook_WithInvalidJson_Returns400()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/breez/webhook", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    public void Dispose()
    {
        _client.Dispose();
        _server.Dispose();
    }
}

/// <summary>
/// DTO for webhook payload used in tests.
/// This mirrors the expected structure from webhook-api.yaml
/// </summary>
public record WebhookPayload
{
    public string EventType { get; init; } = default!;
    public string PaymentHash { get; init; } = default!;
    public ulong? AmountSat { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string? Preimage { get; init; }
    public ulong? FeeSat { get; init; }
    public int? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? CorrelationId { get; init; }
    public string? Invoice { get; init; }
    public string? Description { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public DateTimeOffset? ExpiredAt { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// DTO for webhook response used in tests.
/// </summary>
public record WebhookResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
}
