using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Notification;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Runtime;

namespace Umbraco.Community.Bitcoin.LightningPayments.CoreTests.Services;

public class EmailNotificationHandlerTests
{
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly IOptions<NotificationOptions> _options;
    private readonly Mock<ILogger<EmailNotificationHandler>> _loggerMock;
    private readonly EmailNotificationHandler _sut;

    public EmailNotificationHandlerTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _options = Options.Create(new NotificationOptions());
        _loggerMock = new Mock<ILogger<EmailNotificationHandler>>();
        _sut = new EmailNotificationHandler(_emailServiceMock.Object, _options, _loggerMock.Object);
    }

    [Fact]
    public void HandlerType_ReturnsEmail()
    {
        _sut.HandlerType.Should().Be(NotificationType.Email);
    }

    [Fact]
    public async Task SendAsync_WhenEmailServiceSucceeds_ReturnsSucceeded()
    {
        // Arrange
        var notification = CreateTestNotification();
        _emailServiceMock
            .Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SendAsync(notification);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_WhenEmailServiceThrows_ReturnsFailed()
    {
        // Arrange
        var notification = CreateTestNotification();
        _emailServiceMock
            .Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("SMTP connection failed"));

        // Act
        var result = await _sut.SendAsync(notification);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("SMTP connection failed");
    }

    [Fact]
    public async Task SendAsync_PaymentConfirmed_CorrectSubject()
    {
        // Arrange
        var notification = CreateTestNotification(NotificationEvent.PaymentConfirmed);
        string? capturedSubject = null;
        _emailServiceMock
            .Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((_, subject, _) => capturedSubject = subject)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SendAsync(notification);

        // Assert
        capturedSubject.Should().Be("Lightning Payment Confirmed");
    }

    [Fact]
    public async Task SendAsync_PaymentFailed_CorrectSubject()
    {
        // Arrange
        var notification = CreateTestNotification(NotificationEvent.PaymentFailed);
        string? capturedSubject = null;
        _emailServiceMock
            .Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((_, subject, _) => capturedSubject = subject)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SendAsync(notification);

        // Assert
        capturedSubject.Should().Be("Lightning Payment Failed");
    }

    [Fact]
    public async Task SendAsync_RefundCompleted_CorrectSubject()
    {
        // Arrange
        var notification = CreateTestNotification(NotificationEvent.RefundCompleted);
        string? capturedSubject = null;
        _emailServiceMock
            .Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((_, subject, _) => capturedSubject = subject)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SendAsync(notification);

        // Assert
        capturedSubject.Should().Be("Lightning Refund Completed");
    }

    [Fact]
    public async Task SendAsync_CallsEmailService_WithCorrectDestination()
    {
        // Arrange
        var notification = CreateTestNotification(destination: "custom@example.com");
        _emailServiceMock
            .Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SendAsync(notification);

        // Assert
        _emailServiceMock.Verify(
            s => s.SendEmailAsync("custom@example.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullEmailService_ThrowsArgumentNullException()
    {
        var act = () => new EmailNotificationHandler(null!, _options, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("emailService");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new EmailNotificationHandler(_emailServiceMock.Object, null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new EmailNotificationHandler(_emailServiceMock.Object, _options, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region Helpers

    private static PaymentNotification CreateTestNotification(
        NotificationEvent evt = NotificationEvent.PaymentConfirmed,
        string destination = "admin@test.com") => new()
    {
        NotificationId = Guid.NewGuid(),
        PaymentHash = "test_payment_hash",
        Type = NotificationType.Email,
        Event = evt,
        Destination = destination,
        Status = NotificationStatus.Pending,
        AttemptCount = 1,
        MaxAttempts = 5,
        CreatedAt = DateTimeOffset.UtcNow
    };

    #endregion
}
