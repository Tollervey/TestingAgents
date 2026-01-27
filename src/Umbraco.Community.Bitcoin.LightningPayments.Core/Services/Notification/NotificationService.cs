using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Notification;

/// <summary>
/// Orchestrates payment notification dispatch to configured handlers.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly PaymentDbContext _context;
    private readonly IEnumerable<INotificationHandler> _handlers;
    private readonly IOptions<NotificationOptions> _options;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        PaymentDbContext context,
        IEnumerable<INotificationHandler> handlers,
        IOptions<NotificationOptions> options,
        ILogger<NotificationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SendNotificationAsync(string paymentHash, NotificationEvent notificationEvent, CancellationToken ct = default)
    {
        var config = _options.Value;
        if (!config.Enabled)
        {
            _logger.LogDebug("Notifications disabled, skipping for {PaymentHash}", paymentHash);
            return;
        }

        var payment = await _context.PaymentStates
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PaymentHash == paymentHash, ct);

        if (payment is null)
        {
            throw new PaymentNotFoundException(paymentHash);
        }

        if (!IsEventEnabled(notificationEvent, config))
        {
            _logger.LogDebug("Event {Event} is not enabled, skipping notification", notificationEvent);
            return;
        }

        var notifications = new List<PaymentNotification>();

        if (config.Email.Enabled && !string.IsNullOrEmpty(config.Email.RecipientEmail))
        {
            notifications.Add(CreateNotification(paymentHash, notificationEvent, NotificationType.Email,
                config.Email.RecipientEmail));
        }

        if (config.Webhook.Enabled && !string.IsNullOrEmpty(config.Webhook.Url))
        {
            var payload = WebhookPayloadBuilder.BuildPayload(notificationEvent, payment);
            notifications.Add(CreateNotification(paymentHash, notificationEvent, NotificationType.Webhook,
                config.Webhook.Url, payload));
        }

        if (notifications.Count == 0)
        {
            _logger.LogDebug("No notification channels configured for {PaymentHash}", paymentHash);
            return;
        }

        _context.PaymentNotifications.AddRange(notifications);
        await _context.SaveChangesAsync(ct);

        foreach (var notification in notifications)
        {
            var handler = _handlers.FirstOrDefault(h => h.HandlerType == notification.Type);
            if (handler is null)
            {
                _logger.LogWarning("No handler registered for notification type {Type}", notification.Type);
                continue;
            }

            var result = await handler.SendAsync(notification, ct);
            if (result is not null)
            {
                await UpdateNotificationStatus(notification, result, ct);
            }
        }
    }

    /// <inheritdoc />
    public async Task RetryNotificationAsync(Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _context.PaymentNotifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId, ct);

        if (notification is null)
        {
            throw new PaymentNotFoundException($"Notification {notificationId} not found");
        }

        var handler = _handlers.FirstOrDefault(h => h.HandlerType == notification.Type);
        if (handler is null)
        {
            throw new InvalidOperationException($"No handler registered for notification type {notification.Type}");
        }

        notification.AttemptCount++;
        var result = await handler.SendAsync(notification, ct);
        await UpdateNotificationStatus(notification, result, ct);
    }

    /// <inheritdoc />
    public Task<NotificationOptions> GetConfigurationAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_options.Value);
    }

    /// <inheritdoc />
    public Task<NotificationOptions> UpdateConfigurationAsync(NotificationOptions config, CancellationToken ct = default)
    {
        // Configuration is read-only from appsettings in this implementation.
        // A future version could persist to a config store.
        return Task.FromResult(_options.Value);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<PaymentNotification> Items, int Total)> GetNotificationsAsync(
        NotificationStatus? status = null, int skip = 0, int take = 20, CancellationToken ct = default)
    {
        var query = _context.PaymentNotifications.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(n => n.Status == status.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    private PaymentNotification CreateNotification(
        string paymentHash, NotificationEvent notificationEvent, NotificationType type,
        string destination, string? payload = null)
    {
        return new PaymentNotification
        {
            NotificationId = Guid.NewGuid(),
            PaymentHash = paymentHash,
            Type = type,
            Event = notificationEvent,
            Destination = destination,
            Status = NotificationStatus.Pending,
            AttemptCount = 1,
            MaxAttempts = _options.Value.Retry.MaxAttempts,
            CreatedAt = DateTimeOffset.UtcNow,
            Payload = payload
        };
    }

    private async Task UpdateNotificationStatus(PaymentNotification notification, NotificationDeliveryResult result, CancellationToken ct)
    {
        if (result.Success)
        {
            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTimeOffset.UtcNow;
        }
        else
        {
            notification.LastError = result.ErrorMessage;
            notification.ResponseStatusCode = result.HttpStatusCode;

            if (notification.AttemptCount >= notification.MaxAttempts)
            {
                notification.Status = NotificationStatus.Failed;
                notification.FailedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                notification.Status = NotificationStatus.Retrying;
                var retryOptions = _options.Value.Retry;
                var delay = Math.Min(
                    retryOptions.InitialDelaySeconds * Math.Pow(retryOptions.BackoffMultiplier, notification.AttemptCount - 1),
                    retryOptions.MaxDelaySeconds);
                notification.NextRetryAt = DateTimeOffset.UtcNow.AddSeconds(delay);
            }
        }

        _context.PaymentNotifications.Update(notification);
        await _context.SaveChangesAsync(ct);
    }

    private static bool IsEventEnabled(NotificationEvent notificationEvent, NotificationOptions config)
    {
        var emailFilter = config.Email.Events;
        var webhookFilter = config.Webhook.Events;

        return notificationEvent switch
        {
            NotificationEvent.PaymentConfirmed => emailFilter.PaymentConfirmed || webhookFilter.PaymentConfirmed,
            NotificationEvent.PaymentFailed => emailFilter.PaymentFailed || webhookFilter.PaymentFailed,
            NotificationEvent.PaymentExpired => emailFilter.PaymentExpired || webhookFilter.PaymentExpired,
            NotificationEvent.RefundInitiated => emailFilter.RefundInitiated || webhookFilter.RefundInitiated,
            NotificationEvent.RefundCompleted => emailFilter.RefundCompleted || webhookFilter.RefundCompleted,
            _ => false
        };
    }
}
