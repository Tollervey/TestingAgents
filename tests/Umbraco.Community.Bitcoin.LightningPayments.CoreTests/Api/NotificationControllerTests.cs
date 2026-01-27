using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management.Dto;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Notification;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Api;

public class NotificationControllerTests
{
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly NotificationController _sut;

    public NotificationControllerTests()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _sut = new NotificationController(_notificationServiceMock.Object);
    }

    #region GetNotificationConfig Tests

    [Fact]
    public async Task GetNotificationConfig_ReturnsOkWithConfig()
    {
        // Arrange
        var config = new NotificationOptions
        {
            Enabled = true,
            Email = new EmailNotificationOptions
            {
                Enabled = true,
                RecipientEmail = "admin@test.com",
                Events = new NotificationEventFilter { PaymentConfirmed = true, PaymentFailed = true }
            },
            Webhook = new WebhookNotificationOptions
            {
                Enabled = true,
                Url = "https://webhook.test.com/hook"
            }
        };

        _notificationServiceMock
            .Setup(s => s.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _sut.GetNotificationConfig(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var dto = okResult!.Value as NotificationConfigDto;
        dto.Should().NotBeNull();
        dto!.EmailEnabled.Should().BeTrue();
        dto.WebhookEnabled.Should().BeTrue();
    }

    #endregion

    #region UpdateNotificationConfig Tests

    [Fact]
    public async Task UpdateNotificationConfig_ReturnsOkWithUpdatedConfig()
    {
        // Arrange
        var request = new NotificationConfigDto
        {
            EmailEnabled = true,
            EmailRecipients = new[] { "admin@test.com" },
            WebhookEnabled = false,
            Events = new[] { "payment.confirmed" }
        };

        _notificationServiceMock
            .Setup(s => s.UpdateConfigurationAsync(It.IsAny<NotificationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationOptions
            {
                Enabled = true,
                Email = new EmailNotificationOptions
                {
                    Enabled = true,
                    RecipientEmail = "admin@test.com",
                    Events = new NotificationEventFilter { PaymentConfirmed = true }
                }
            });

        // Act
        var result = await _sut.UpdateNotificationConfig(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region ListNotifications Tests

    [Fact]
    public async Task ListNotifications_ReturnsOkWithItems()
    {
        // Arrange
        var notifications = new List<PaymentNotification>
        {
            CreateTestNotification("hash1", NotificationStatus.Sent),
            CreateTestNotification("hash2", NotificationStatus.Failed)
        };

        _notificationServiceMock
            .Setup(s => s.GetNotificationsAsync(null, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((notifications.AsReadOnly() as IReadOnlyList<PaymentNotification>, 2));

        // Act
        var result = await _sut.ListNotifications(status: null, skip: 0, take: 20, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as NotificationListResponseDto;
        response.Should().NotBeNull();
        response!.Total.Should().Be(2);
        response.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListNotifications_WithStatusFilter_ParsesAndPassesStatus()
    {
        // Arrange
        _notificationServiceMock
            .Setup(s => s.GetNotificationsAsync(NotificationStatus.Failed, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<PaymentNotification>() as IReadOnlyList<PaymentNotification>, 0));

        // Act
        var result = await _sut.ListNotifications(status: "Failed", skip: 0, take: 20, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _notificationServiceMock.Verify(
            s => s.GetNotificationsAsync(NotificationStatus.Failed, 0, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListNotifications_EmptyList_ReturnsOkWithZeroTotal()
    {
        // Arrange
        _notificationServiceMock
            .Setup(s => s.GetNotificationsAsync(null, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<PaymentNotification>() as IReadOnlyList<PaymentNotification>, 0));

        // Act
        var result = await _sut.ListNotifications(status: null, skip: 0, take: 20, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as NotificationListResponseDto;
        response!.Total.Should().Be(0);
        response.Items.Should().BeEmpty();
    }

    #endregion

    #region RetryNotification Tests

    [Fact]
    public async Task RetryNotification_ExistingNotification_ReturnsAccepted()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        _notificationServiceMock
            .Setup(s => s.RetryNotificationAsync(notificationId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.RetryNotification(notificationId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<AcceptedResult>();
    }

    [Fact]
    public async Task RetryNotification_NonExistent_ReturnsNotFound()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        _notificationServiceMock
            .Setup(s => s.RetryNotificationAsync(notificationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentNotFoundException($"Notification {notificationId} not found"));

        // Act
        var result = await _sut.RetryNotification(notificationId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullService_ThrowsArgumentNullException()
    {
        var act = () => new NotificationController(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("notificationService");
    }

    #endregion

    #region Helpers

    private static PaymentNotification CreateTestNotification(string paymentHash, NotificationStatus status) => new()
    {
        NotificationId = Guid.NewGuid(),
        PaymentHash = paymentHash,
        Type = NotificationType.Email,
        Event = NotificationEvent.PaymentConfirmed,
        Destination = "admin@test.com",
        Status = status,
        AttemptCount = 1,
        MaxAttempts = 5,
        CreatedAt = DateTimeOffset.UtcNow,
        SentAt = status == NotificationStatus.Sent ? DateTimeOffset.UtcNow : null
    };

    #endregion
}
