using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Notification;

/// <summary>
/// Handles delivery of a specific notification type (email or webhook).
/// </summary>
public interface INotificationHandler
{
    /// <summary>
    /// The notification type this handler supports.
    /// </summary>
    NotificationType HandlerType { get; }

    /// <summary>
    /// Send a notification.
    /// </summary>
    /// <returns>Result of the delivery attempt.</returns>
    Task<NotificationDeliveryResult> SendAsync(PaymentNotification notification, CancellationToken ct = default);
}

/// <summary>
/// Result of a notification delivery attempt.
/// </summary>
public record NotificationDeliveryResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int? HttpStatusCode { get; init; }

    public static NotificationDeliveryResult Succeeded() => new() { Success = true };

    public static NotificationDeliveryResult Failed(string error, int? statusCode = null)
        => new() { Success = false, ErrorMessage = error, HttpStatusCode = statusCode };
}
