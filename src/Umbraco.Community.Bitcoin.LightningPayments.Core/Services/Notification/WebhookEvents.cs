namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Services.Notification;

/// <summary>
/// Base webhook envelope per webhook-api.yaml WebhookEnvelope schema.
/// </summary>
public record WebhookEnvelope
{
    public string Event { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public Guid WebhookId { get; init; }
    public int AttemptNumber { get; init; } = 1;
    public object? Data { get; init; }
}

/// <summary>
/// Payment confirmed webhook event data.
/// </summary>
public record PaymentConfirmedEventData
{
    public string PaymentHash { get; init; } = string.Empty;
    public long AmountSat { get; init; }
    public FiatAmountData? AmountFiat { get; init; }
    public string? Kind { get; init; }
    public int? ContentId { get; init; }
    public string? ContentName { get; init; }
    public string? UserSessionId { get; init; }
    public DateTimeOffset? InvoiceCreatedAt { get; init; }
    public DateTimeOffset ConfirmedAt { get; init; }
    public string? Tier { get; init; }
    public DateTimeOffset? AccessExpiresAt { get; init; }
    public Guid? Bolt12OfferId { get; init; }
}

/// <summary>
/// Payment failed webhook event data.
/// </summary>
public record PaymentFailedEventData
{
    public string PaymentHash { get; init; } = string.Empty;
    public long? AmountSat { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }
    public int? ContentId { get; init; }
    public string? UserSessionId { get; init; }
    public DateTimeOffset FailedAt { get; init; }
}

/// <summary>
/// Refund completed webhook event data.
/// </summary>
public record RefundCompletedEventData
{
    public Guid RefundId { get; init; }
    public string OriginalPaymentHash { get; init; } = string.Empty;
    public long AmountSat { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public string? RefundPaymentHash { get; init; }
    public DateTimeOffset CompletedAt { get; init; }
}

/// <summary>
/// Fiat amount data included in webhook payloads.
/// </summary>
public record FiatAmountData
{
    public string Currency { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
