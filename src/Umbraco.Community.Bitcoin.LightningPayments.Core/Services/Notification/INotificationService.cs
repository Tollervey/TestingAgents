using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Notification;

/// <summary>
/// Orchestrates payment notification dispatch (email and webhook).
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send notifications for a payment event.
    /// Creates notification records and dispatches to configured handlers.
    /// </summary>
    Task SendNotificationAsync(string paymentHash, NotificationEvent notificationEvent, CancellationToken ct = default);

    /// <summary>
    /// Retry a previously failed notification.
    /// </summary>
    Task RetryNotificationAsync(Guid notificationId, CancellationToken ct = default);

    /// <summary>
    /// Get notification configuration.
    /// </summary>
    Task<NotificationOptions> GetConfigurationAsync(CancellationToken ct = default);

    /// <summary>
    /// Update notification configuration.
    /// </summary>
    Task<NotificationOptions> UpdateConfigurationAsync(NotificationOptions config, CancellationToken ct = default);

    /// <summary>
    /// List notifications with optional filtering.
    /// </summary>
    Task<(IReadOnlyList<PaymentNotification> Items, int Total)> GetNotificationsAsync(
        NotificationStatus? status = null, int skip = 0, int take = 20, CancellationToken ct = default);
}
