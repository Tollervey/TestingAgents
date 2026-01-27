using System.Text.Json;
using FluentAssertions;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management.Dto;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Api.Contract;

public class NotificationContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    #region NotificationConfigDto Contract Tests

    [Fact]
    public void NotificationConfigDto_Serialization_MatchesContract()
    {
        // Arrange
        var config = new NotificationConfigDto
        {
            EmailEnabled = true,
            EmailRecipients = new[] { "admin@test.com" },
            WebhookEnabled = true,
            WebhookUrl = "https://webhook.test.com/hook",
            Events = new[] { "payment.confirmed", "payment.failed" }
        };

        // Act
        var json = JsonSerializer.Serialize(config, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<NotificationConfigDto>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.EmailEnabled.Should().BeTrue();
        deserialized.EmailRecipients.Should().Contain("admin@test.com");
        deserialized.WebhookEnabled.Should().BeTrue();
        deserialized.WebhookUrl.Should().Be("https://webhook.test.com/hook");
        deserialized.Events.Should().HaveCount(2);

        // Verify camelCase
        json.Should().Contain("\"emailEnabled\":");
        json.Should().Contain("\"webhookEnabled\":");
        json.Should().Contain("\"webhookUrl\":");
    }

    [Fact]
    public void NotificationConfigDto_WebhookSecret_NullWhenNotSet()
    {
        // Arrange - WebhookSecret has JsonIgnore(WhenWritingNull)
        var config = new NotificationConfigDto
        {
            EmailEnabled = false,
            WebhookEnabled = false,
            WebhookSecret = null
        };

        // Act
        var json = JsonSerializer.Serialize(config, JsonOptions);

        // Assert
        json.Should().NotContain("\"webhookSecret\":");
    }

    #endregion

    #region NotificationListResponseDto Contract Tests

    [Fact]
    public void NotificationListResponseDto_Serialization_MatchesContract()
    {
        // Arrange
        var response = new NotificationListResponseDto
        {
            Items = new[]
            {
                new NotificationSummaryResponse
                {
                    NotificationId = Guid.NewGuid(),
                    PaymentHash = "abc123",
                    Type = "email",
                    Event = "payment.confirmed",
                    Status = "sent",
                    AttemptCount = 1,
                    CreatedAt = DateTimeOffset.UtcNow,
                    SentAt = DateTimeOffset.UtcNow
                }
            },
            Total = 1
        };

        // Act
        var json = JsonSerializer.Serialize(response, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<NotificationListResponseDto>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Total.Should().Be(1);
        deserialized.Items.Should().HaveCount(1);

        json.Should().Contain("\"items\":");
        json.Should().Contain("\"total\":1");
    }

    #endregion

    #region NotificationSummaryResponse Contract Tests

    [Fact]
    public void NotificationSummaryResponse_Serialization_MatchesContract()
    {
        // Arrange
        var notificationId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        var createdAt = new DateTimeOffset(2026, 1, 27, 12, 0, 0, TimeSpan.Zero);

        var summary = new NotificationSummaryResponse
        {
            NotificationId = notificationId,
            PaymentHash = "3df0ae",
            Type = "webhook",
            Event = "payment.confirmed",
            Status = "sent",
            AttemptCount = 1,
            CreatedAt = createdAt,
            SentAt = createdAt,
            LastError = null
        };

        // Act
        var json = JsonSerializer.Serialize(summary, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<NotificationSummaryResponse>(json, JsonOptions);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.NotificationId.Should().Be(notificationId);
        deserialized.PaymentHash.Should().Be("3df0ae");
        deserialized.Type.Should().Be("webhook");
        deserialized.Event.Should().Be("payment.confirmed");
        deserialized.Status.Should().Be("sent");
        deserialized.AttemptCount.Should().Be(1);

        // Verify camelCase property names
        json.Should().Contain("\"notificationId\":");
        json.Should().Contain("\"paymentHash\":");
        json.Should().Contain("\"type\":");
        json.Should().Contain("\"status\":");
        json.Should().Contain("\"attemptCount\":");
    }

    [Fact]
    public void NotificationSummaryResponse_NullableFields_HandleCorrectly()
    {
        // Arrange
        var summary = new NotificationSummaryResponse
        {
            NotificationId = Guid.NewGuid(),
            PaymentHash = "test",
            Type = "email",
            Event = "payment.failed",
            Status = "failed",
            AttemptCount = 5,
            CreatedAt = DateTimeOffset.UtcNow,
            SentAt = null,
            LastError = "SMTP timeout"
        };

        // Act
        var json = JsonSerializer.Serialize(summary, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<NotificationSummaryResponse>(json, JsonOptions);

        // Assert
        deserialized!.SentAt.Should().BeNull();
        deserialized.LastError.Should().Be("SMTP timeout");
    }

    [Theory]
    [InlineData("payment.confirmed")]
    [InlineData("payment.failed")]
    [InlineData("payment.expired")]
    [InlineData("refund.initiated")]
    [InlineData("refund.completed")]
    public void NotificationSummaryResponse_EventValues_MatchWebhookApiSpec(string eventType)
    {
        // Arrange
        var summary = new NotificationSummaryResponse
        {
            NotificationId = Guid.NewGuid(),
            PaymentHash = "test",
            Type = "webhook",
            Event = eventType,
            Status = "sent",
            AttemptCount = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(summary, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<NotificationSummaryResponse>(json, JsonOptions);

        // Assert
        deserialized!.Event.Should().Be(eventType);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("sent")]
    [InlineData("retrying")]
    [InlineData("failed")]
    public void NotificationSummaryResponse_StatusValues_MatchSpec(string status)
    {
        // Arrange
        var summary = new NotificationSummaryResponse
        {
            NotificationId = Guid.NewGuid(),
            PaymentHash = "test",
            Type = "email",
            Event = "payment.confirmed",
            Status = status,
            AttemptCount = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(summary, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<NotificationSummaryResponse>(json, JsonOptions);

        // Assert
        deserialized!.Status.Should().Be(status);
    }

    [Theory]
    [InlineData("email")]
    [InlineData("webhook")]
    public void NotificationSummaryResponse_TypeValues_MatchSpec(string type)
    {
        // Arrange
        var summary = new NotificationSummaryResponse
        {
            NotificationId = Guid.NewGuid(),
            PaymentHash = "test",
            Type = type,
            Event = "payment.confirmed",
            Status = "sent",
            AttemptCount = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var json = JsonSerializer.Serialize(summary, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<NotificationSummaryResponse>(json, JsonOptions);

        // Assert
        deserialized!.Type.Should().Be(type);
    }

    #endregion
}
