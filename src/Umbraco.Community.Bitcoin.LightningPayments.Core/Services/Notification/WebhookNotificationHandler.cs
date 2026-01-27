using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Configuration;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Notification;

/// <summary>
/// Handles webhook notification delivery with HMAC-SHA256 signing.
/// Per webhook-api.yaml signature verification spec.
/// </summary>
public class WebhookNotificationHandler : INotificationHandler
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<NotificationOptions> _options;
    private readonly ILogger<WebhookNotificationHandler> _logger;

    public WebhookNotificationHandler(
        HttpClient httpClient,
        IOptions<NotificationOptions> options,
        ILogger<WebhookNotificationHandler> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public NotificationType HandlerType => NotificationType.Webhook;

    /// <inheritdoc />
    public async Task<NotificationDeliveryResult> SendAsync(PaymentNotification notification, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(notification.Destination))
        {
            return NotificationDeliveryResult.Failed("Webhook URL is not configured");
        }

        var webhookOptions = _options.Value.Webhook;

        try
        {
            var payload = notification.Payload ?? string.Empty;
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            using var request = new HttpRequestMessage(HttpMethod.Post, notification.Destination);
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            // Add signature headers per webhook-api.yaml spec
            request.Headers.Add("X-Lightning-Timestamp", timestamp);
            request.Headers.Add("X-Lightning-Webhook-Id", notification.NotificationId.ToString());
            request.Headers.Add("X-Lightning-Event", WebhookPayloadBuilder.MapEventToString(notification.Event));
            request.Headers.Add("X-Lightning-Attempt", notification.AttemptCount.ToString());

            if (!string.IsNullOrEmpty(webhookOptions.Secret))
            {
                var signature = ComputeHmacSignature(timestamp, payload, webhookOptions.Secret);
                request.Headers.Add("X-Lightning-Signature", signature);
            }

            var response = await _httpClient.SendAsync(request, ct);
            var statusCode = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Webhook notification {NotificationId} delivered to {Destination} with status {StatusCode}",
                    notification.NotificationId, notification.Destination, statusCode);

                return NotificationDeliveryResult.Succeeded();
            }

            _logger.LogWarning(
                "Webhook notification {NotificationId} to {Destination} returned status {StatusCode}",
                notification.NotificationId, notification.Destination, statusCode);

            return NotificationDeliveryResult.Failed(
                $"Webhook returned HTTP {statusCode}",
                statusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to deliver webhook notification {NotificationId} to {Destination}",
                notification.NotificationId, notification.Destination);

            return NotificationDeliveryResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Compute HMAC-SHA256 signature per webhook-api.yaml spec:
    /// HMAC-SHA256(secret, timestamp + "." + body) -> hex-encoded lowercase
    /// </summary>
    public static string ComputeHmacSignature(string timestamp, string body, string secret)
    {
        var payload = $"{timestamp}.{body}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
