using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Runtime;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Notification;

/// <summary>
/// Handles email notification delivery using IEmailService.
/// </summary>
public class EmailNotificationHandler : INotificationHandler
{
    private readonly IEmailService _emailService;
    private readonly IOptions<NotificationOptions> _options;
    private readonly ILogger<EmailNotificationHandler> _logger;

    public EmailNotificationHandler(
        IEmailService emailService,
        IOptions<NotificationOptions> options,
        ILogger<EmailNotificationHandler> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public NotificationType HandlerType => NotificationType.Email;

    /// <inheritdoc />
    public async Task<NotificationDeliveryResult> SendAsync(PaymentNotification notification, CancellationToken ct = default)
    {
        try
        {
            var subject = BuildSubject(notification.Event);
            var body = BuildBody(notification);

            await _emailService.SendEmailAsync(notification.Destination, subject, body);

            _logger.LogInformation(
                "Email notification {NotificationId} sent to {Destination} for event {Event}",
                notification.NotificationId, notification.Destination, notification.Event);

            return NotificationDeliveryResult.Succeeded();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email notification {NotificationId} to {Destination}",
                notification.NotificationId, notification.Destination);

            return NotificationDeliveryResult.Failed(ex.Message);
        }
    }

    private static string BuildSubject(NotificationEvent notificationEvent) => notificationEvent switch
    {
        NotificationEvent.PaymentConfirmed => "Lightning Payment Confirmed",
        NotificationEvent.PaymentFailed => "Lightning Payment Failed",
        NotificationEvent.PaymentExpired => "Lightning Invoice Expired",
        NotificationEvent.RefundInitiated => "Lightning Refund Initiated",
        NotificationEvent.RefundCompleted => "Lightning Refund Completed",
        _ => "Lightning Payment Notification"
    };

    private static string BuildBody(PaymentNotification notification)
    {
        return $"""
            Payment Notification
            ====================
            Event: {WebhookPayloadBuilder.MapEventToString(notification.Event)}
            Payment Hash: {notification.PaymentHash}
            Time: {notification.CreatedAt:u}

            This is an automated notification from Lightning Payments.
            """;
    }
}
