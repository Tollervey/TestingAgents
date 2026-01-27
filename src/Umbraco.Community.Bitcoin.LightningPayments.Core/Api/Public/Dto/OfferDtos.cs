using Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Shared;

namespace Umbraco.Community.Bitcoin.LightningPayments.Core.Api.Public.Dto;

/// <summary>
/// Public-facing offer info DTO.
/// Maps to OfferInfo schema in public-api.yaml.
/// </summary>
public record OfferInfo
{
    public string OfferString { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ulong? AmountSat { get; init; }
    public FiatAmount? AmountFiat { get; init; }
    public bool IsActive { get; init; }
}
