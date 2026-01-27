using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Base;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management.Dto;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Data.Models;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Exceptions;
using Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Notification;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management;

/// <summary>
/// Management API controller for notification configuration and delivery history.
/// </summary>
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Lightning Payments Notifications")]
public class NotificationController : OurUmbracoBitcoinLightningPaymentsApiControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    /// <summary>
    /// Get notification configuration.
    /// </summary>
    [HttpGet("notifications/config")]
    [ProducesResponseType<NotificationConfigDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationConfigDto>> GetNotificationConfig(CancellationToken ct = default)
    {
        var config = await _notificationService.GetConfigurationAsync(ct);
        return Ok(MapToConfigDto(config));
    }

    /// <summary>
    /// Update notification configuration.
    /// </summary>
    [HttpPut("notifications/config")]
    [ProducesResponseType<NotificationConfigDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationConfigDto>> UpdateNotificationConfig(
        [FromBody] NotificationConfigDto request,
        CancellationToken ct = default)
    {
        var config = MapFromConfigDto(request);
        var updated = await _notificationService.UpdateConfigurationAsync(config, ct);
        return Ok(MapToConfigDto(updated));
    }

    /// <summary>
    /// List notification delivery history.
    /// </summary>
    [HttpGet("notifications")]
    [ProducesResponseType<NotificationListResponseDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationListResponseDto>> ListNotifications(
        [FromQuery] string? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        NotificationStatus? parsedStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<NotificationStatus>(status, ignoreCase: true, out var s))
        {
            parsedStatus = s;
        }

        var (items, total) = await _notificationService.GetNotificationsAsync(parsedStatus, skip, take, ct);

        return Ok(new NotificationListResponseDto
        {
            Items = items.Select(MapToSummaryResponse).ToList(),
            Total = total
        });
    }

    /// <summary>
    /// Manually retry a failed notification.
    /// </summary>
    [HttpPost("notifications/{notificationId:guid}/retry")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryNotification(Guid notificationId, CancellationToken ct = default)
    {
        try
        {
            await _notificationService.RetryNotificationAsync(notificationId, ct);
            return Accepted();
        }
        catch (PaymentNotFoundException)
        {
            return NotFound();
        }
    }

    private static NotificationConfigDto MapToConfigDto(Configuration.NotificationOptions config) => new()
    {
        EmailEnabled = config.Email.Enabled,
        EmailRecipients = config.Email.RecipientEmail is not null
            ? new[] { config.Email.RecipientEmail }
            : Array.Empty<string>(),
        WebhookEnabled = config.Webhook.Enabled,
        WebhookUrl = config.Webhook.Url,
        Events = GetEnabledEvents(config)
    };

    private static Configuration.NotificationOptions MapFromConfigDto(NotificationConfigDto dto)
    {
        var config = new Configuration.NotificationOptions
        {
            Enabled = dto.EmailEnabled || dto.WebhookEnabled,
            Email = new Configuration.EmailNotificationOptions
            {
                Enabled = dto.EmailEnabled,
                RecipientEmail = dto.EmailRecipients.FirstOrDefault()
            },
            Webhook = new Configuration.WebhookNotificationOptions
            {
                Enabled = dto.WebhookEnabled,
                Url = dto.WebhookUrl,
                Secret = dto.WebhookSecret
            }
        };

        foreach (var evt in dto.Events)
        {
            switch (evt)
            {
                case "payment.confirmed":
                    config.Email.Events.PaymentConfirmed = true;
                    config.Webhook.Events.PaymentConfirmed = true;
                    break;
                case "payment.failed":
                    config.Email.Events.PaymentFailed = true;
                    config.Webhook.Events.PaymentFailed = true;
                    break;
                case "refund.completed":
                    config.Email.Events.RefundCompleted = true;
                    config.Webhook.Events.RefundCompleted = true;
                    break;
            }
        }

        return config;
    }

    private static string[] GetEnabledEvents(Configuration.NotificationOptions config)
    {
        var events = new List<string>();
        var filter = config.Email.Events;
        if (filter.PaymentConfirmed) events.Add("payment.confirmed");
        if (filter.PaymentFailed) events.Add("payment.failed");
        if (filter.RefundCompleted) events.Add("refund.completed");
        return events.ToArray();
    }

    private static NotificationSummaryResponse MapToSummaryResponse(PaymentNotification n) => new()
    {
        NotificationId = n.NotificationId,
        PaymentHash = n.PaymentHash,
        Type = n.Type.ToString().ToLowerInvariant(),
        Event = WebhookPayloadBuilder.MapEventToString(n.Event),
        Status = n.Status.ToString().ToLowerInvariant(),
        AttemptCount = n.AttemptCount,
        CreatedAt = n.CreatedAt,
        SentAt = n.SentAt,
        LastError = n.LastError
    };
}
