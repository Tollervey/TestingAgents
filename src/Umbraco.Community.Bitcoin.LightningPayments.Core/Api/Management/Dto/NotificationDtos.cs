using System.Text.Json.Serialization;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management.Dto;

/// <summary>
/// Notification configuration DTO.
/// Maps to NotificationConfig schema in management-api.yaml.
/// </summary>
public record NotificationConfigDto
{
    public bool EmailEnabled { get; init; }
    public IReadOnlyList<string> EmailRecipients { get; init; } = Array.Empty<string>();
    public bool WebhookEnabled { get; init; }
    public string? WebhookUrl { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WebhookSecret { get; init; }

    public IReadOnlyList<string> Events { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Response DTO for notification list.
/// Maps to NotificationListResponse schema in management-api.yaml.
/// </summary>
public record NotificationListResponseDto
{
    public IReadOnlyList<NotificationSummaryResponse> Items { get; init; } = Array.Empty<NotificationSummaryResponse>();
    public int Total { get; init; }
}

/// <summary>
/// Response DTO for a notification summary.
/// Maps to NotificationSummary schema in management-api.yaml.
/// </summary>
public record NotificationSummaryResponse
{
    public Guid NotificationId { get; init; }
    public string PaymentHash { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("event")]
    public string Event { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;
    public int AttemptCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? SentAt { get; init; }
    public string? LastError { get; init; }
}
