namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management.Dto;

/// <summary>
/// Response DTO for a refund transaction.
/// Maps to RefundTransaction schema in management-api.yaml.
/// </summary>
public record RefundTransactionResponse
{
    public Guid RefundId { get; init; }
    public string OriginalPaymentHash { get; init; } = string.Empty;
    public long AmountSat { get; init; }
    public string DestinationInvoice { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public string? Reason { get; init; }
    public string InitiatedByUserId { get; init; } = string.Empty;
    public DateTimeOffset InitiatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? RefundPaymentHash { get; init; }
}

/// <summary>
/// Paginated list of refund transactions.
/// Maps to RefundListResponse schema in management-api.yaml.
/// </summary>
public record RefundListResponse
{
    public IReadOnlyList<RefundTransactionResponse> Items { get; init; } = Array.Empty<RefundTransactionResponse>();
    public int Total { get; init; }
}

/// <summary>
/// Request DTO for initiating a refund.
/// Maps to InitiateRefundRequest schema in management-api.yaml.
/// </summary>
public record InitiateRefundRequest
{
    /// <summary>
    /// Payment hash of the original payment to refund (required).
    /// </summary>
    public string OriginalPaymentHash { get; init; } = string.Empty;

    /// <summary>
    /// BOLT11 invoice from customer for the refund (required).
    /// </summary>
    public string DestinationInvoice { get; init; } = string.Empty;

    /// <summary>
    /// Optional reason for the refund (max 500 chars).
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// Request DTO for preparing/validating a refund before execution.
/// Maps to PrepareRefundRequest schema in management-api.yaml.
/// </summary>
public record PrepareRefundRequest
{
    /// <summary>
    /// Payment hash of the original payment to refund (required).
    /// </summary>
    public string OriginalPaymentHash { get; init; } = string.Empty;

    /// <summary>
    /// BOLT11 invoice from customer for the refund (required).
    /// </summary>
    public string DestinationInvoice { get; init; } = string.Empty;
}

/// <summary>
/// Response DTO for refund preparation with fee estimate.
/// Maps to PrepareRefundResponse schema in management-api.yaml.
/// </summary>
public record PrepareRefundResponse
{
    public long OriginalAmountSat { get; init; }
    public long RefundAmountSat { get; init; }
    public long FeeSat { get; init; }
    public long WalletBalanceSat { get; init; }
    public bool CanProceed { get; init; }
    public string? ValidationError { get; init; }
}
