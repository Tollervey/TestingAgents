using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Notification;

/// <summary>
/// Background service that processes the notification retry queue.
/// Polls for notifications in Retrying status whose NextRetryAt has passed.
/// </summary>
public class NotificationRetryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<NotificationOptions> _options;
    private readonly ILogger<NotificationRetryBackgroundService> _logger;
    private readonly TimeSpan _pollInterval;

    public NotificationRetryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationOptions> options,
        ILogger<NotificationRetryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pollInterval = TimeSpan.FromSeconds(30);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Notification retry service disabled (notifications not enabled)");
            return;
        }

        _logger.LogInformation("Notification retry background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRetryQueue(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification retry queue");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Notification retry background service stopped");
    }

    private async Task ProcessRetryQueue(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var handlers = scope.ServiceProvider.GetRequiredService<IEnumerable<INotificationHandler>>();

        var pendingRetries = await context.PaymentNotifications
            .Where(n => n.Status == NotificationStatus.Retrying &&
                        n.NextRetryAt != null &&
                        n.NextRetryAt <= DateTimeOffset.UtcNow)
            .OrderBy(n => n.NextRetryAt)
            .Take(100)
            .ToListAsync(ct);

        if (pendingRetries.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} notification retries", pendingRetries.Count);

        foreach (var notification in pendingRetries)
        {
            var handler = handlers.FirstOrDefault(h => h.HandlerType == notification.Type);
            if (handler is null)
            {
                _logger.LogWarning("No handler for notification type {Type}", notification.Type);
                continue;
            }

            notification.AttemptCount++;
            var result = await handler.SendAsync(notification, ct);

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
                    _logger.LogWarning(
                        "Notification {NotificationId} permanently failed after {Attempts} attempts",
                        notification.NotificationId, notification.AttemptCount);
                }
                else
                {
                    var retryOptions = _options.Value.Retry;
                    var delay = Math.Min(
                        retryOptions.InitialDelaySeconds * Math.Pow(retryOptions.BackoffMultiplier, notification.AttemptCount - 1),
                        retryOptions.MaxDelaySeconds);
                    notification.NextRetryAt = DateTimeOffset.UtcNow.AddSeconds(delay);
                }
            }
        }

        await context.SaveChangesAsync(ct);
    }
}
