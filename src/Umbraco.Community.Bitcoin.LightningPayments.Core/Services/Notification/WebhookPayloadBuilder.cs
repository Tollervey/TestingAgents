using System.Text.Json;
using System.Text.Json.Serialization;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Notification;

/// <summary>
/// Builds webhook payloads for payment notification events.
/// </summary>
public static class WebhookPayloadBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Build a webhook payload for the given notification event and payment.
    /// </summary>
    public static string BuildPayload(NotificationEvent notificationEvent, PaymentState payment, int attemptNumber = 1)
    {
        var envelope = new WebhookEnvelope
        {
            Event = MapEventToString(notificationEvent),
            Timestamp = DateTimeOffset.UtcNow,
            WebhookId = Guid.NewGuid(),
            AttemptNumber = attemptNumber,
            Data = BuildEventData(notificationEvent, payment)
        };

        return JsonSerializer.Serialize(envelope, JsonOptions);
    }

    /// <summary>
    /// Map a notification event enum to the webhook event string.
    /// </summary>
    public static string MapEventToString(NotificationEvent notificationEvent) => notificationEvent switch
    {
        NotificationEvent.PaymentConfirmed => "payment.confirmed",
        NotificationEvent.PaymentFailed => "payment.failed",
        NotificationEvent.PaymentExpired => "payment.expired",
        NotificationEvent.RefundInitiated => "refund.initiated",
        NotificationEvent.RefundCompleted => "refund.completed",
        _ => throw new ArgumentOutOfRangeException(nameof(notificationEvent))
    };

    private static object BuildEventData(NotificationEvent notificationEvent, PaymentState payment)
    {
        return notificationEvent switch
        {
            NotificationEvent.PaymentConfirmed => new PaymentConfirmedEventData
            {
                PaymentHash = payment.PaymentHash,
                AmountSat = (long)payment.AmountSat,
                Kind = payment.Kind.ToString().ToLowerInvariant(),
                ContentId = payment.ContentId == 0 ? null : payment.ContentId,
                UserSessionId = payment.UserSessionId,
                ConfirmedAt = DateTimeOffset.UtcNow,
                Bolt12OfferId = payment.Bolt12OfferId
            },
            NotificationEvent.PaymentFailed => new PaymentFailedEventData
            {
                PaymentHash = payment.PaymentHash,
                AmountSat = (long)payment.AmountSat,
                Reason = "Payment failed",
                ContentId = payment.ContentId == 0 ? null : payment.ContentId,
                UserSessionId = payment.UserSessionId,
                FailedAt = DateTimeOffset.UtcNow
            },
            _ => new { paymentHash = payment.PaymentHash }
        };
    }
}
