namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Management.Dto;

/// <summary>
/// Response DTO for a BOLT12 offer (list item).
/// Maps to Bolt12Offer schema in management-api.yaml.
/// </summary>
public record Bolt12OfferResponse
{
    public Guid OfferId { get; init; }
    public string OfferString { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ulong? AmountSat { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? DeactivatedAt { get; init; }
    public int? ContentId { get; init; }
}

/// <summary>
/// Response DTO for a BOLT12 offer with payment summary details.
/// Maps to Bolt12OfferDetails schema in management-api.yaml (extends Bolt12Offer).
/// </summary>
public record Bolt12OfferDetailsResponse : Bolt12OfferResponse
{
    public int PaymentCount { get; init; }
    public long TotalReceivedSat { get; init; }
    public int ConfirmedCount { get; init; }
}

/// <summary>
/// Request DTO for creating a new BOLT12 offer.
/// Maps to CreateOfferRequest schema in management-api.yaml.
/// </summary>
public record CreateOfferRequest
{
    /// <summary>
    /// Human-readable description (required, max 200 chars).
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Fixed amount in satoshis. Null for variable amount offers.
    /// </summary>
    public ulong? AmountSat { get; init; }

    /// <summary>
    /// Optional content ID to associate with the offer.
    /// </summary>
    public int? ContentId { get; init; }
}
