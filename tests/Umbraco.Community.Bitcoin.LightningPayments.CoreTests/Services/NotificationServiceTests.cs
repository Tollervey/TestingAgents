using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Notification;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services;

public class NotificationServiceTests : IDisposable
{
    private readonly PaymentDbContext _context;
    private readonly Mock<INotificationHandler> _emailHandlerMock;
    private readonly Mock<INotificationHandler> _webhookHandlerMock;
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly INotificationService _sut;
    private readonly IOptions<NotificationOptions> _options;

    public NotificationServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase($"NotificationServiceTest_{Guid.NewGuid()}")
            .Options;
        _context = new PaymentDbContext(dbOptions);
        _context.Database.EnsureCreated();

        _emailHandlerMock = new Mock<INotificationHandler>();
        _emailHandlerMock.Setup(h => h.HandlerType).Returns(NotificationType.Email);

        _webhookHandlerMock = new Mock<INotificationHandler>();
        _webhookHandlerMock.Setup(h => h.HandlerType).Returns(NotificationType.Webhook);

        _loggerMock = new Mock<ILogger<NotificationService>>();
        _options = CreateOptions();

        _sut = new NotificationService(
            _context,
            new[] { _emailHandlerMock.Object, _webhookHandlerMock.Object },
            _options,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region SendNotificationAsync Tests

    [Fact]
    public async Task SendNotificationAsync_WhenDisabled_DoesNotCreateNotifications()
    {
        // Arrange
        var options = CreateOptions(enabled: false);
        var sut = new NotificationService(_context,
            new[] { _emailHandlerMock.Object }, options, _loggerMock.Object);
        await SeedPayment("hash1");

        // Act
        await sut.SendNotificationAsync("hash1", NotificationEvent.PaymentConfirmed);

        // Assert
        var count = await _context.PaymentNotifications.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task SendNotificationAsync_WhenPaymentNotFound_ThrowsPaymentNotFoundException()
    {
        // Act
        var act = async () => await _sut.SendNotificationAsync("nonexistent", NotificationEvent.PaymentConfirmed);

        // Assert
        await act.Should().ThrowAsync<PaymentNotFoundException>();
    }

    [Fact]
    public async Task SendNotificationAsync_WithEmailEnabled_CreatesEmailNotification()
    {
        // Arrange
        await SeedPayment("hash_email");
        _emailHandlerMock
            .Setup(h => h.SendAsync(It.IsAny<PaymentNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationDeliveryResult.Succeeded());

        // Act
        await _sut.SendNotificationAsync("hash_email", NotificationEvent.PaymentConfirmed);

        // Assert
        var notifications = await _context.PaymentNotifications.ToListAsync();
        notifications.Should().Contain(n => n.Type == NotificationType.Email);
        _emailHandlerMock.Verify(h => h.SendAsync(It.IsAny<PaymentNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendNotificationAsync_WithWebhookEnabled_CreatesWebhookNotificationWithPayload()
    {
        // Arrange
        await SeedPayment("hash_webhook");
        _webhookHandlerMock
            .Setup(h => h.SendAsync(It.IsAny<PaymentNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationDeliveryResult.Succeeded());

        // Act
        await _sut.SendNotificationAsync("hash_webhook", NotificationEvent.PaymentConfirmed);

        // Assert
        var notification = await _context.PaymentNotifications
            .FirstOrDefaultAsync(n => n.Type == NotificationType.Webhook);
        notification.Should().NotBeNull();
        notification!.Payload.Should().NotBeNullOrEmpty();
        notification.Destination.Should().Be("https://webhook.test.com/hook");
    }

    [Fact]
    public async Task SendNotificationAsync_WithBothEnabled_CreatesBothNotifications()
    {
        // Arrange
        await SeedPayment("hash_both");
        _emailHandlerMock
            .Setup(h => h.SendAsync(It.IsAny<PaymentNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationDeliveryResult.Succeeded());
        _webhookHandlerMock
            .Setup(h => h.SendAsync(It.IsAny<PaymentNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationDeliveryResult.Succeeded());

        // Act
        await _sut.SendNotificationAsync("hash_both", NotificationEvent.PaymentConfirmed);

        // Assert
        var notifications = await _context.PaymentNotifications.ToListAsync();
        notifications.Should().HaveCount(2);
        notifications.Should().Contain(n => n.Type == NotificationType.Email);
        notifications.Should().Contain(n => n.Type == NotificationType.Webhook);
    }

    [Fact]
    public async Task SendNotificationAsync_WhenHandlerSucceeds_SetsStatusToSent()
    {
        // Arrange
        await SeedPayment("hash_sent");
        _emailHandlerMock
            .Setup(h => h.SendAsync(It.IsAny<PaymentNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationDeliveryResult.Succeeded());

        // Act
        await _sut.SendNotificationAsync("hash_sent", NotificationEvent.PaymentConfirmed);

        // Assert
        var notification = await _context.PaymentNotifications
            .FirstOrDefaultAsync(n => n.Type == NotificationType.Email);
        notification!.Status.Should().Be(NotificationStatus.Sent);
        notification.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SendNotificationAsync_WhenHandlerFailsWithRetriesRemaining_SetsStatusToRetrying()
    {
        // Arrange
        await SeedPayment("hash_retry");
        _emailHandlerMock
            .Setup(h => h.SendAsync(It.IsAny<PaymentNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationDeliveryResult.Failed("SMTP error"));

        // Act
        await _sut.SendNotificationAsync("hash_retry", NotificationEvent.PaymentConfirmed);

        // Assert
        var notification = await _context.PaymentNotifications
            .FirstOrDefaultAsync(n => n.Type == NotificationType.Email);
        notification!.Status.Should().Be(NotificationStatus.Retrying);
        notification.NextRetryAt.Should().NotBeNull();
        notification.LastError.Should().Be("SMTP error");
    }

    [Fact]
    public async Task SendNotificationAsync_WhenEventNotEnabled_DoesNotCreateNotifications()
    {
        // Arrange - PaymentExpired is disabled by default in our test options
        await SeedPayment("hash_disabled_event");

        // Act
        await _sut.SendNotificationAsync("hash_disabled_event", NotificationEvent.PaymentExpired);

        // Assert
        var count = await _context.PaymentNotifications.CountAsync();
        count.Should().Be(0);
    }

    #endregion

    #region RetryNotificationAsync Tests

    [Fact]
    public async Task RetryNotificationAsync_WhenNotificationExists_IncrementsAttemptCount()
    {
        // Arrange
        var notification = await SeedNotification("hash_retry_existing", NotificationStatus.Retrying, attemptCount: 2);
        _emailHandlerMock
            .Setup(h => h.SendAsync(It.IsAny<PaymentNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationDeliveryResult.Succeeded());

        // Act
        await _sut.RetryNotificationAsync(notification.NotificationId);

        // Assert
        var updated = await _context.PaymentNotifications.FindAsync(notification.NotificationId);
        updated!.AttemptCount.Should().Be(3);
        updated.Status.Should().Be(NotificationStatus.Sent);
    }

    [Fact]
    public async Task RetryNotificationAsync_WhenNotificationNotFound_ThrowsPaymentNotFoundException()
    {
        // Act
        var act = async () => await _sut.RetryNotificationAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<PaymentNotFoundException>();
    }

    #endregion

    #region GetNotificationsAsync Tests

    [Fact]
    public async Task GetNotificationsAsync_ReturnsCorrectTotalAndItems()
    {
        // Arrange
        await SeedNotification("hash_list_1", NotificationStatus.Sent);
        await SeedNotification("hash_list_2", NotificationStatus.Failed);
        await SeedNotification("hash_list_3", NotificationStatus.Sent);

        // Act
        var (items, total) = await _sut.GetNotificationsAsync();

        // Assert
        total.Should().Be(3);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithStatusFilter_FiltersCorrectly()
    {
        // Arrange
        await SeedNotification("hash_filter_1", NotificationStatus.Sent);
        await SeedNotification("hash_filter_2", NotificationStatus.Failed);

        // Act
        var (items, total) = await _sut.GetNotificationsAsync(status: NotificationStatus.Sent);

        // Assert
        total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].Status.Should().Be(NotificationStatus.Sent);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (var i = 0; i < 5; i++)
        {
            await SeedNotification($"hash_page_{i}", NotificationStatus.Sent);
        }

        // Act
        var (items, total) = await _sut.GetNotificationsAsync(skip: 2, take: 2);

        // Assert
        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    #endregion

    #region GetConfigurationAsync Tests

    [Fact]
    public async Task GetConfigurationAsync_ReturnsCurrentOptions()
    {
        // Act
        var config = await _sut.GetConfigurationAsync();

        // Assert
        config.Should().NotBeNull();
        config.Enabled.Should().BeTrue();
        config.Email.Enabled.Should().BeTrue();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => new NotificationService(
            null!, new[] { _emailHandlerMock.Object }, _options, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullHandlers_ThrowsArgumentNullException()
    {
        var act = () => new NotificationService(
            _context, null!, _options, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("handlers");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        var act = () => new NotificationService(
            _context, new[] { _emailHandlerMock.Object }, null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var act = () => new NotificationService(
            _context, new[] { _emailHandlerMock.Object }, _options, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region Helpers

    private static IOptions<NotificationOptions> CreateOptions(
        bool enabled = true, bool emailEnabled = true, bool webhookEnabled = true)
    {
        var options = new NotificationOptions
        {
            Enabled = enabled,
            Email = new EmailNotificationOptions
            {
                Enabled = emailEnabled,
                RecipientEmail = "admin@test.com",
                Events = new NotificationEventFilter
                {
                    PaymentConfirmed = true,
                    PaymentFailed = true,
                    PaymentExpired = false
                }
            },
            Webhook = new WebhookNotificationOptions
            {
                Enabled = webhookEnabled,
                Url = "https://webhook.test.com/hook",
                Secret = "test-secret",
                Events = new NotificationEventFilter
                {
                    PaymentConfirmed = true,
                    PaymentFailed = true,
                    PaymentExpired = false
                }
            },
            Retry = new NotificationRetryOptions
            {
                MaxAttempts = 5,
                InitialDelaySeconds = 30,
                MaxDelaySeconds = 3600,
                BackoffMultiplier = 2.0
            }
        };
        return Options.Create(options);
    }

    private async Task<PaymentState> SeedPayment(string paymentHash)
    {
        var payment = new PaymentState
        {
            PaymentHash = paymentHash,
            Status = PaymentStatus.Paid,
            AmountSat = 5000,
            ContentId = 42,
            UserSessionId = $"session-{Guid.NewGuid()}",
            Kind = PaymentKind.Paywall
        };
        _context.PaymentStates.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    private async Task<PaymentNotification> SeedNotification(
        string paymentHash, NotificationStatus status, int attemptCount = 1)
    {
        // Ensure payment exists
        if (!await _context.PaymentStates.AnyAsync(p => p.PaymentHash == paymentHash))
        {
            await SeedPayment(paymentHash);
        }

        var notification = new PaymentNotification
        {
            NotificationId = Guid.NewGuid(),
            PaymentHash = paymentHash,
            Type = NotificationType.Email,
            Event = NotificationEvent.PaymentConfirmed,
            Destination = "admin@test.com",
            Status = status,
            AttemptCount = attemptCount,
            MaxAttempts = 5,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _context.PaymentNotifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    #endregion
}
